using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentinel.Configs;
using Sentinel.Helpers;

namespace Sentinel.Services
{
    public class StateService
    {
        private readonly ILogger<StateService> _logger;
        private readonly SystemConfig _systemConfig;
        private readonly IHostEnvironment _hostEnvironment;

        public long InstanceId { get; } = new Random().NextInt64();
        public bool IsLastHeartbeatSucceeded { get; set; } = false;
        public bool IsFirstReport { get; set; } = false;

        public SystemConfig SystemConfig => _systemConfig;
        public string AccessToken { get; set; } = string.Empty;
        private string _refreshToken = string.Empty;
        public string RefreshToken
        {
            get => _refreshToken;
            set
            {
                if (value != _refreshToken)
                {
                    _logger.LogDebug("Updating refresh token in config");
                    _systemConfig.LibrarianRefreshToken = value;
                    AppSettingsHelper.UpdateSystemConfig(_systemConfig, _hostEnvironment);
                    _refreshToken = value;
                }
            }
        }

        public StateService(ILogger<StateService> logger, SystemConfig systemConfig, IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _systemConfig = systemConfig;
            _hostEnvironment = hostEnvironment;

            _refreshToken = _systemConfig.LibrarianRefreshToken;
        }

        public void SetTokens((string accessToken, string refreshToken) tokens)
        {
            AccessToken = tokens.accessToken;
            RefreshToken = tokens.refreshToken;
        }
    }
}
