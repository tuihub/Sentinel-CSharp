using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using System.Reflection;
using System.Windows.Input;

namespace Sentinel
{
    internal class Program
    {
        private static ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        private static ILogger _logger;
        private static IServiceProvider _serviceProvider;

        private static IList<Type> _pluginTypeList = new List<Type>();

        static void Main(string[] args)
        {
            _logger = _loggerFactory.CreateLogger<Program>();

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());

            LoadPlugins(services);

            _serviceProvider = services.BuildServiceProvider();

            var logger2 = _serviceProvider.GetRequiredService<ILogger<Program>>();

            var plugins = _serviceProvider.GetServices<IPlugin>();
            var options = plugins.Select(p => p.CommandLineOptions).ToArray();
            var optionTypes = options.Select(o => o.GetType()).ToArray();

            Parser.Default.ParseArguments(args, optionTypes)
                          .WithParsed(Run);
        }

        private static void Run(object obj)
        {
            throw new NotImplementedException();
        }

        private static void LoadPlugins(ServiceCollection services)
        {
            string[] pluginPaths = new[]
            {
                "./plugins/Sentinel.Plugin.SingleFile/Sentinel.Plugin.SingleFile.dll"
            };

            foreach (var path in pluginPaths)
            {
                Assembly pluginAssembly = LoadPlugin(path);
                GetIPlugins(pluginAssembly, services);
            }
        }

        private static void GetIPlugins(Assembly assembly, ServiceCollection services)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    try
                    {
                        services.AddSingleton(typeof(IPlugin), type);
                        _pluginTypeList.Add(type);
                        _logger.LogInformation($"Loaded IPlugin {type} from {assembly.FullName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to load plugin: {type.FullName}");
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