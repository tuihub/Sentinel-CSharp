using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentinel.Configs;
using Sentinel.Helpers;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using Sentinel.Workers;

namespace Sentinel
{
    class Program
    {
        private static IServiceProvider s_pluginServiceProvider = null!;
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

                var pluginServices = new ServiceCollection();

                var systemConfig = builder.Configuration.GetSection("SystemConfig").Get<SystemConfig>()
                    ?? throw new Exception("Failed to parse SystemConfig");

                // load plugins
                PluginHelper.LoadPlugins(s_logger, pluginServices, systemConfig.PluginBaseDir);

                // load config & register worker
                var libraryConfig = builder.Configuration.GetSection("LibraryConfig");
                s_pluginServiceProvider = pluginServices.BuildServiceProvider();
                s_plugins = s_pluginServiceProvider.GetServices<IPlugin>();
                foreach (var config in libraryConfig.GetChildren())
                {
                    var pluginName = config.GetSection("PluginName").Get<string>();
                    var plugin = s_plugins.FirstOrDefault(p => p.Name == pluginName)
                        ?? throw new Exception($"Failed to find plugin with name {pluginName}");
                    plugin.Config = config.GetSection("PluginConfig").Get(plugin.Config.GetType()) as PluginConfigBase
                        ?? throw new Exception($"Failed to parse PluginConfig for {pluginName}");

                    builder.Services.AddHostedService(w => new FSScanWorker(
                        (IPlugin)s_pluginServiceProvider.GetRequiredService(plugin.GetType()),
                        plugin.Config));
                }

                IHost host = builder.Build();
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
                    if (argsList.Count < pluginBaseDirIndex + 2) { throw new Exception("Missing plugin base dir"); }
                    pluginBaseDir = argsList[pluginBaseDirIndex + 1];
                    if (pluginBaseDir.StartsWith('-')) { throw new Exception("Missing plugin base dir"); }
                    argsList.RemoveRange(pluginBaseDirIndex, 2);
                }
                if (debug) { argsList.Remove("--debug"); }

                var pluginServices = new ServiceCollection();
                // load plugins
                PluginHelper.LoadPlugins(s_logger, pluginServices, pluginBaseDir);

                // add logger
                pluginServices.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                    if (debug) { builder.SetMinimumLevel(LogLevel.Debug); }
                });

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
                var appBinaries = plugin.GetSentinelAppBinaries(options);
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