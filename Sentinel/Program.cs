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

        static void Main(string[] args)
        {
            _logger = _loggerFactory.CreateLogger("Program");

            LoadPlugins();
        }

        private static void LoadPlugins()
        {
            string[] pluginPaths = new[]
            {
                "./plugins/Sentinel.Plugin.SingleFile/Sentinel.Plugin.SingleFile.dll"
            };

            List<IPlugin> plugins = new();
            foreach (var path in pluginPaths)
            {
                Assembly pluginAssembly = LoadPlugin(path);
                plugins.AddRange(GetIPlugins(pluginAssembly));
            }
        }

        private static IEnumerable<IPlugin> GetIPlugins(Assembly assembly)
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    var instance = Activator.CreateInstance(type, new object[] { _loggerFactory });
                    IPlugin? result = instance as IPlugin;
                    if (result != null)
                    {
                        count++;
                        yield return result;
                    }
                }
            }

            if (count == 0)
            {
                string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
                throw new ApplicationException(
                    $"Can't find any type which implements ICommand in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
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