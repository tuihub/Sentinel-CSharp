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
using Sentinel.Services;
using Sentinel.Workers;
using System.Diagnostics;
using TuiHub.Protos.Librarian.Sephirah.V1;

namespace Sentinel
{
    class Program
    {
        private static ILogger s_logger = null!;
        private static ServiceCollection s_pluginServices = new ServiceCollection();

        static void Main(string[] args)
        {
            bool debug = args.Any(x => x == "--debug");
            s_logger = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                if (debug) { builder.SetMinimumLevel(LogLevel.Debug); }
            }).CreateLogger<Program>();

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

            // add services & built-in plugins
            s_pluginServices.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                if (debug) { builder.SetMinimumLevel(LogLevel.Debug); }
            });
            s_pluginServices.AddTransient<IPlugin, Plugin.SingleFile.SingleFile>();
            s_pluginServices.AddTransient<IPlugin, Plugin.PythonPluginLoader.PythonPluginLoader>();

            Parser.Default.ParseArguments<DaemonOptions, Plugin.SingleFile.CmdOptions, Plugin.PythonPluginLoader.CmdOptions>(argsList)
                .WithParsed<DaemonOptions>(o => RunDaemon(o, args))
                .WithParsed<Plugin.SingleFile.CmdOptions>(o => RunOnce(o))
                .WithParsed<Plugin.PythonPluginLoader.CmdOptions>(o => RunOnce(o))
                .WithNotParsed(e => RunWithExtPlugins(pluginBaseDir, argsList));
        }

        private static void RunWithExtPlugins(string pluginBaseDir, List<string> argsList)
        {
            PluginHelper.LoadPlugins(s_logger, s_pluginServices, pluginBaseDir);
            var pluginServiceProvider = s_pluginServices.BuildServiceProvider();
            var optionTypes = pluginServiceProvider.GetServices<IPlugin>()
                .Select(p => p.CmdOptions)
                .Select(o => o.GetType());
            Parser.Default.ParseArguments(argsList, optionTypes.ToArray())
                          .WithParsed(o => RunOnce((o as CmdOptionsBase)!));
        }

        private static void RunOnce(CmdOptionsBase options)
        {
            var pluginServiceProvider = s_pluginServices.BuildServiceProvider();
            var plugin = pluginServiceProvider.GetServices<IPlugin>()
                .Where(p => p.CmdOptions.GetType() == options.GetType()).Single();
            try
            {
                plugin.SetConfig(options);
                var result = plugin.DoFullScan(Enumerable.Empty<Plugin.Models.AppBinary>().AsQueryable());
                s_logger.LogInformation($"Plugin {plugin.Name} finished with {result.AppBinariesToAdd.Count()} entries.");
                s_logger.LogInformation(string.Join(Environment.NewLine, result.AppBinariesToAdd.Select(x => x.ToFullHumanString())));
            }
            catch (Exception ex)
            {
                s_logger.LogError(ex, $"Error running plugin {plugin.Name}");
                Environment.Exit(1);
            }
        }

        private static void RunDaemon(DaemonOptions options, string[] args)
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
            if (options.NoReportToServer)
            {
                builder.Services.AddSingleton<LoggingOnlyInterceptor>();
                builder.Services.AddGrpcClient<LibrarianSephirahService.LibrarianSephirahServiceClient>(o =>
                {
                    o.Address = new Uri("http://127.0.0.1");
                })
                .AddInterceptor<LoggingOnlyInterceptor>();
            }
            else
            {
                builder.Services.AddSingleton<ClientTokenInterceptor>();
                builder.Services.AddGrpcClient<LibrarianSephirahService.LibrarianSephirahServiceClient>(o =>
                {
                    o.Address = new Uri(systemConfig.LibrarianUrl);
                })
                .AddInterceptor<ClientTokenInterceptor>();
            }
            builder.Services.AddSingleton<LibrarianClientService>();

            // load built-in plugins
            builder.Services.AddKeyedTransient<IPlugin, Plugin.SingleFile.SingleFile>("SingleFile");
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
                    var config = libraryConfig.PluginConfig.Get<ConfigBase>()
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

            host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopped.Register(() =>
            {
                s_logger.LogInformation("Host is stopped.");

                // https://github.com/pythonnet/pythonnet/issues/2008
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
    }
}