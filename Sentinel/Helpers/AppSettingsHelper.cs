using Microsoft.Extensions.Hosting;
using Sentinel.Configs;
using System.Text.Json;

namespace Sentinel.Helpers
{
    public static class AppSettingsHelper
    {
        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

        public static void UpdateSystemConfig(SystemConfig systemConfig, IHostEnvironment hostEnvironment)
        {
            var appSettingsPath = Path.Join(hostEnvironment.ContentRootPath, $"appsettings.{hostEnvironment.EnvironmentName}.json");
            if (!File.Exists(appSettingsPath))
            {
                appSettingsPath = Path.Join(hostEnvironment.ContentRootPath, "appsettings.json");
            }
            var appSettingsObj = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(appSettingsPath));
            appSettingsObj!.SystemConfig = systemConfig;
            File.WriteAllText(appSettingsPath, JsonSerializer.Serialize(appSettingsObj, s_jsonSerializerOptions));
        }
    }
}
