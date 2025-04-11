using Microsoft.Extensions.Hosting;
using Sentinel.Configs;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sentinel.Helpers
{
    public static class AppSettingsHelper
    {
        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

        public static void UpdateSystemConfig(SystemConfig systemConfig, IHostEnvironment hostEnvironment)
        {
            var appSettingsPath = Path.Join(hostEnvironment.ContentRootPath, $"appsettings.{hostEnvironment.EnvironmentName}.json");
            if (!File.Exists(appSettingsPath) || JsonNode.Parse(File.ReadAllText(appSettingsPath))?["SystemConfig"] == null)
            {
                appSettingsPath = Path.Join(hostEnvironment.ContentRootPath, "appsettings.json");
            }

            var jsonNode = JsonNode.Parse(File.ReadAllText(appSettingsPath))!;
            jsonNode["SystemConfig"]!["LibrarianRefreshToken"] = systemConfig.LibrarianRefreshToken;
            File.WriteAllText(appSettingsPath, jsonNode.ToJsonString(s_jsonSerializerOptions));
        }
    }
}
