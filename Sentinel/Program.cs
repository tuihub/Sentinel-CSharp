using CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentinel.Configs;
using Sentinel.Helpers;
using Sentinel.Interceptors;
using Sentinel.Models.Db;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Options;
using Sentinel.Services;
using Sentinel.Workers;
using System.Diagnostics;
using TuiHub.Protos.Librarian.Sephirah.V1;

namespace Sentinel
{
    class Program
    {
        private static ServiceProvider s_pluginServiceProvider = null!;
        private static IEnumerable<IPlugin> s_plugins = null!;
        private static ILogger s_logger = null!;

        static void Main(string[] args)
        {
            bool debug = args.Any(x => x == "--debug");
            s_logger = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                if (debug) { builder.SetMinimumLevel(LogLevel.Debug); }
            }).CreateLogger<Program>();

            if (args.Any(x => x == "daemon" || x == "d"))
            {
                var builder = Host.CreateApplicationBuilder(args);

                // get config
                var systemConfig = builder.Configuration.GetSection("SystemConfig").Get<SystemConfig>()
                    ?? throw new ArgumentException("Failed to parse SystemConfig");
                builder.Services.AddSingleton(systemConfig);
                var sentinelConfig = builder.Configuration.GetSection("SentinelConfig").Get<SentinelConfig>()
                    ?? throw new ArgumentException("Failed to parse SentinelConfig");
                builder.Services.AddSingleton(sentinelConfig);

                // add db context
                builder.Services.AddDbContext<SentinelDbContext>(o => o.UseSqlite($"Data Source={systemConfig.DbPath}"));

                // add state service
                builder.Services.AddSingleton<StateService>();

                // add grpc services
                builder.Services.AddSingleton<ClientTokenInterceptor>();
                builder.Services.AddSingleton<LoggingOnlyInterceptor>();
                builder.Services.AddGrpcClient<LibrarianSephirahService.LibrarianSephirahServiceClient>(o =>
                    {
                        o.Address = new Uri(systemConfig.LibrarianUrl);
                    })
                .AddInterceptor<ClientTokenInterceptor>();
                //.AddInterceptor<LoggingOnlyInterceptor>();
                builder.Services.AddSingleton<LibrarianClientService>();

                // load built-in plugins
                //builder.Services.AddKeyedTransient<IPlugin, Plugin.SingleFile.SingleFile>("SingleFile");
                builder.Services.AddKeyedTransient<IPlugin, Plugin.PythonPluginLoader.PythonPluginLoader>("PythonPluginLoader");

                // load plugins
                PluginHelper.LoadPlugins(s_logger, builder.Services, systemConfig.PluginBaseDir);

                // load config & register worker
                var libraryConfigs = systemConfig.LibraryConfigs;
                foreach (var config in libraryConfigs)
                {
                    // fswatcher not implemented
                    //builder.Services.AddHostedService<FSWatchWorker>(p => new FSWatchWorker(
                    //    p.GetRequiredService<ILogger<FSWatchWorker>>(),
                    //    p.GetRequiredService<SentinelDbContext>(),
                    //    new FSScanWorker(
                    //        p.GetRequiredService<ILogger<FSWatchWorker>>(),
                    //        p.GetRequiredService<SentinelDbContext>(),
                    //        plugin),
                    //    plugin.Config.LibraryFolder));

                    builder.Services.AddHostedService<ScheduledFSScanWorker>(p => new ScheduledFSScanWorker(
                        p.GetRequiredService<ILogger<ScheduledFSScanWorker>>(),
                        p.GetRequiredService<IServiceScopeFactory>(),
                        p.GetRequiredKeyedService<IPlugin>(config.PluginName),
                        config.PluginConfig,
                        p.GetRequiredService<LibrarianClientService>(),
                        TimeSpan.FromMinutes(systemConfig.LibraryScanIntervalMinutes)));
                }

                IHost host = builder.Build();

                // ensure db & init
                using (var scope = host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<SentinelDbContext>();
                    // migrate db
                    dbContext.Database.Migrate();
                    // init base dirs
                    foreach (var libraryConfig in libraryConfigs)
                    {
                        var config = libraryConfig.PluginConfig.Get<PluginConfigBase>()
                            ?? throw new ArgumentException("Failed to parse PluginConfig");
                        var path = config.LibraryFolder;
                        if (!dbContext.AppBinaryBaseDirs.Any(d => d.Path == path))
                        {
                            dbContext.AppBinaryBaseDirs.Add(new AppBinaryBaseDir
                            {
                                Name = config.LibraryName,
                                Path = path
                            });
                        }
                    }
                    dbContext.SaveChanges();

                    // report sentinel info
                    var librarianClientService = scope.ServiceProvider.GetRequiredService<LibrarianClientService>();
                    librarianClientService.ReportSentinelInformationAsync().Wait();
                }

                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                lifetime.ApplicationStopped.Register(() =>
                {
                    s_logger.LogInformation("Host is stopped.");
                    
                    if (libraryConfigs.Any(x => x.PluginName == "PythonPluginLoader"))
                    {
                        Task.Run(async () =>
                        {
                            var timeout = TimeSpan.FromSeconds(10);
                            s_logger.LogWarning($"Python plugin loader detected, forcing close in {timeout.TotalSeconds:F2} seconds.");
                            await Task.Delay(timeout);
                            Process.GetCurrentProcess().Kill();
                        });
                    }
                });

                host.Run();
            }
            else
            {
                // parse args
                var argsList = args.ToList();
                string pluginBaseDir;
                int pluginBaseDirIndex = argsList.FindIndex(x => x == "--plugin-base-dir");
                if (pluginBaseDirIndex == -1) { pluginBaseDir = "./plugins"; }
                else
                {
                    if (argsList.Count < pluginBaseDirIndex + 2)
                    {
                        throw new Exception("Missing plugin base dir");
                    }
                    pluginBaseDir = argsList[pluginBaseDirIndex + 1];
                    if (pluginBaseDir.StartsWith('-')) { throw new Exception("Missing plugin base dir"); }
                    argsList.RemoveRange(pluginBaseDirIndex, 2);
                }
                if (debug) { argsList.Remove("--debug"); }

                var pluginServices = new ServiceCollection();

                // add logger
                pluginServices.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                    if (debug) { builder.SetMinimumLevel(LogLevel.Debug); }
                });

                // load built-in plugins
                pluginServices.AddTransient<IPlugin, Plugin.SingleFile.SingleFile>();

                // load plugins
                PluginHelper.LoadPlugins(s_logger, pluginServices, pluginBaseDir);

                // build plugin service provider
                s_pluginServiceProvider = pluginServices.BuildServiceProvider();
                s_plugins = s_pluginServiceProvider.GetServices<IPlugin>();

                // parse CommandLineOptions
                var optionTypes = s_plugins
                    .Select(p => p.CommandLineOptions)
                    .Select(o => o.GetType())
                    .ToArray();
                Parser.Default.ParseArguments(argsList, optionTypes)
                              .WithParsed(Run);
            }
        }

        private static void Run(object obj)
        {
            var plugin = s_plugins.Where(p => p.CommandLineOptions.GetType() == obj.GetType()).FirstOrDefault();
            if (plugin == null)
            {
                s_logger.LogError($"Failed to find plugin for CommandLineOptions type {obj.GetType().FullName}");
                Environment.Exit(1);
            }
            try
            {
                CommandLineOptionsBase options = (CommandLineOptionsBase)obj;
                var appBinaries = plugin.GetAppBinaries(options);
                if (options.PrintToConsole == true)
                {
                    foreach (var appBinary in appBinaries)
                    {
                        Console.WriteLine(appBinary);
                        foreach (var fileEntry in appBinary.Files)
                        {
                            Console.WriteLine(fileEntry + $", Sha256 = {BitConverter.ToString(fileEntry.Sha256).Replace("-", "")}");
                            foreach (var chunk in fileEntry.Chunks)
                            {
                                Console.WriteLine(chunk + $", Sha256 = {BitConverter.ToString(chunk.Sha256).Replace("-", "")}");
                            }
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                s_logger.LogError(ex, $"Failed to run plugin {plugin.Name}");
                Environment.Exit(1);
            }
        }
    }
}