using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Contracts;
using System.Reflection;
using System.Runtime.Loader;

namespace Sentinel.Helpers
{
    public static class PluginHelper
    {
        public static void LoadPlugins(ILogger? logger, IServiceCollection services, string pluginBaseDir)
        {
            var pluginPathList = new List<string>();
            logger?.LogDebug($"Searching dlls from: {pluginBaseDir}");
            foreach (var filePath in Directory.EnumerateFiles(pluginBaseDir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                pluginPathList.Add(filePath);
                logger?.LogDebug($"Added dll: {filePath}");
            }
            logger?.LogDebug($"Searching level 1 subdirectories from: {pluginBaseDir}");
            var l1SubDirs = Directory.EnumerateDirectories(pluginBaseDir, "*", SearchOption.TopDirectoryOnly);
            foreach (var dir in l1SubDirs)
            {
                var dirName = new DirectoryInfo(dir).Name;
                string filePath = Path.Combine(dir, dirName + ".dll");
                if (File.Exists(filePath))
                {
                    pluginPathList.Add(filePath);
                    logger?.LogDebug($"Added dll: {filePath}");
                }
            }

            foreach (var path in pluginPathList)
            {
                try
                {
                    logger?.LogDebug($"Loading dll: {path}");
                    Assembly pluginAssembly = LoadPlugin(logger, path);
                    GetIPlugins(logger, services, pluginAssembly);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, $"Failed to load dll: {path}");
                }
            }
        }

        private static void GetIPlugins(ILogger? logger, IServiceCollection services, Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    try
                    {
                        services.AddSingleton(typeof(IPlugin), type);
                        logger?.LogInformation($"Loaded IPlugin {type} from {assembly.FullName}");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"Failed to load plugin: {type.FullName}");
                    }
                }
            }
        }

        private static Assembly LoadPlugin(ILogger? logger, string path)
        {
            string? curAssemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            string pluginLocation = Path.GetFullPath(Path.Combine(curAssemblyPath ?? string.Empty, path));
            logger?.LogDebug($"Loading IPlugins from: {pluginLocation}");
            var loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }
    }

    class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
