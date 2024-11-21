using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;
using System.Reflection;
using System.Windows.Input;

namespace Sentinel
{
    internal class Program
    {
        private static readonly string _pluginBaseDir = "./plugins";

        private static ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(o =>
        {
            o.IncludeScopes = true;
            o.SingleLine = true;
            o.TimestampFormat = "yyyy-MM-dd HH:mm:sszzz ";
        }));
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
            _services.AddLogging(builder => builder.AddSimpleConsole(o =>
            {
                o.IncludeScopes = true;
                o.SingleLine = true;
                o.TimestampFormat = "yyyy-MM-dd HH:mm:sszzz ";
            }));
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
            try
            {
                OptionsBase options = (OptionsBase)obj;
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

        private static void LoadPlugins()
        {
            var pluginPathList = new List<string>();
            _logger.LogInformation($"Searching dlls from: {_pluginBaseDir}");
            foreach (var filePath in Directory.EnumerateFiles(_pluginBaseDir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                pluginPathList.Add(filePath);
                _logger.LogInformation($"Added dll: {filePath}");
            }
            _logger.LogInformation($"Searching level 1 subdirectories from: {_pluginBaseDir}");
            var l1SubDirs = Directory.EnumerateDirectories(_pluginBaseDir, "*", SearchOption.TopDirectoryOnly);
            foreach (var dir in l1SubDirs)
            {
                var dirName = new DirectoryInfo(dir).Name;
                string filePath = Path.Combine(dir, dirName + ".dll");
                if (File.Exists(filePath))
                {
                    pluginPathList.Add(filePath);
                    _logger.LogInformation($"Added dll: {filePath}");
                }
            }

            foreach (var path in pluginPathList)
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