using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using System.Reflection;
using System.Windows.Input;

namespace Sentinel
{
    internal class Program
    {
        private static ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        private static ILogger _logger;
        private static IServiceCollection _services;
        private static IServiceProvider _serviceProvider;

        private static IEnumerable<IPlugin> _plugins;

        static void Main(string[] args)
        {
            // get Program logger
            _logger = _loggerFactory.CreateLogger<Program>();
            // add services for DI
            _services = new ServiceCollection();
            _services.AddLogging(builder => builder.AddConsole());
            LoadPlugins();
            _serviceProvider = _services.BuildServiceProvider();
            // get CommandLineOptions from plugins
            _plugins = _serviceProvider.GetServices<IPlugin>();
            var optionTypes = _plugins.Select(p => p.CommandLineOptions).Select(o => o.GetType()).ToArray();
            // parse CommandLineOptions
            Parser.Default.ParseArguments(args, optionTypes)
                          .WithParsed(Run);
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
            OptionsBase options = (OptionsBase)obj;
            var entries = plugin.GetEntries();
            if (options.PrintToConsole == true)
            {
                foreach (var entry in entries)
                {
                    Console.WriteLine(entry);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void LoadPlugins()
        {
            string[] pluginPaths = new[]
            {
                "./plugins/Sentinel.Plugin.SingleFile/Sentinel.Plugin.SingleFile.dll"
            };

            foreach (var path in pluginPaths)
            {
                Assembly pluginAssembly = LoadPlugin(path);
                GetIPlugins(pluginAssembly);
            }
        }

        private static void GetIPlugins(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    try
                    {
                        _services.AddSingleton(typeof(IPlugin), type);
                        _logger.LogInformation($"Loaded IPlugin {type} from {assembly.FullName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to load plugin: {type.FullName}");
                    }
                }
            }
        }

        private static Assembly LoadPlugin(string path)
        {
            string curAssemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
            string pluginLocation = Path.GetFullPath(Path.Combine(curAssemblyPath, path.Replace('\\', Path.DirectorySeparatorChar)));
            _logger.LogInformation($"Loading IPlugins from: {pluginLocation}");
            PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }
    }
}