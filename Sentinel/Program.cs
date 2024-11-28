using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Sentinel.Configs;
using Sentinel.Helpers;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using Sentinel.Workers;
using System.Reflection;
using System.Windows.Input;

namespace Sentinel
{
    class Program
    {
        private static IEnumerable<IPlugin> s_plugins = [];
        private static ILogger s_logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();

        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var pluginServices = new ServiceCollection();

            var systemConfig = builder.Configuration.GetSection("SystemConfig").Get<SystemConfig>() ?? throw new Exception("Failed to parse SystemConfig");

            // load plugins
            PluginHelper.LoadPlugins(s_logger, pluginServices, systemConfig.PluginBaseDir);

            // load config & register worker
            var libraryConfig = builder.Configuration.GetSection("LibraryConfig");
            s_plugins = pluginServices.BuildServiceProvider().GetServices<IPlugin>();
            foreach (var config in libraryConfig.GetChildren())
            {
                var pluginName = config.GetSection("PluginName").Get<string>();
                var plugin = s_plugins.FirstOrDefault(p => p.Name == pluginName)
                    ?? throw new Exception($"Failed to find plugin with name {pluginName}");
                plugin.Config = config.GetSection("PluginConfig").Get(plugin.Config.GetType())
                    ?? throw new Exception($"Failed to parse PluginConfig for {pluginName}");

                builder.Services.AddHostedService(w => new FSScanWorker(plugin, plugin.Config));
            }

            // parse CommandLineOptions
            var optionTypes = s_plugins.Select(p => p.CommandLineOptions).Select(o => o.GetType()).Append(typeof(DaemonModeOptions)).ToArray();
            Parser.Default.ParseArguments(args, optionTypes)
                          .WithParsed(Run);

            IHost host = builder.Build();

            host.Run();

        }

        private static void Run(object obj)
        {
            var plugin = _plugins.Where(p => p.CommandLineOptions.GetType() == obj.GetType())
                                 .FirstOrDefault();
            if (plugin == null)
            {
                _logger.LogError($"Failed to find plugin for CommandLineOptions type {obj.GetType().FullName}");
                Environment.Exit(1);
            }
            try
            {
                CommandLineOptionsBase options = (CommandLineOptionsBase)obj;
                var appBinaries = plugin.GetSentinelAppBinaries(obj);
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
                _logger.LogError(ex, $"Failed to run plugin {plugin.Name}");
                Environment.Exit(1);
            }
        }
    }
}