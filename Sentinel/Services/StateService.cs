using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentinel.Configs;
using Sentinel.Models.Db;

namespace Sentinel.Services
{
    public class StateService
    {
        private readonly ILogger<StateService> _logger;
        private readonly SystemConfig _systemConfig;
        private readonly IDbContextFactory<SentinelDbContext> _dbContextFactory;

        public long InstanceId { get; } = new Random().NextInt64();
        public bool IsLastHeartbeatSucceeded { get; set; } = false;
        public bool IsFirstReport { get; set; } = false;

        public SystemConfig SystemConfig => _systemConfig;
        private string _accessToken = string.Empty;
        public string AccessToken
        {
            get => _accessToken;
            set
            {
                if (value != _accessToken)
                {
                    _logger.LogDebug("Updating access token in database");
                    UpdateTokensInDb(value, RefreshToken);
                    _accessToken = value;
                }
            }
        }
        private string _refreshToken = string.Empty;
        public string RefreshToken
        {
            get => _refreshToken;
            set
            {
                if (value != _refreshToken)
                {
                    _logger.LogDebug("Updating refresh token in database");
                    UpdateTokensInDb(AccessToken, value);
                    _refreshToken = value;
                }
            }
        }

        public StateService(ILogger<StateService> logger, SystemConfig systemConfig, IDbContextFactory<SentinelDbContext> dbContextFactory)
        {
            _logger = logger;
            _systemConfig = systemConfig;
            _dbContextFactory = dbContextFactory;

            // Load tokens from database
            LoadTokensFromDb();
        }

        public void SetTokens((string accessToken, string refreshToken) tokens)
        {
            AccessToken = tokens.accessToken;
            RefreshToken = tokens.refreshToken;
        }

        private void LoadTokensFromDb()
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var token = dbContext.AuthTokens.FirstOrDefault();
                if (token != null)
                {
                    AccessToken = token.AccessToken;
                    _refreshToken = token.RefreshToken;
                    _logger.LogDebug("Loaded token from database");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading token from database");
            }
        }

        private void UpdateTokensInDb(string accessToken, string refreshToken)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                
                var existingToken = dbContext.AuthTokens.FirstOrDefault();
                if (existingToken != null)
                {
                    existingToken.AccessToken = accessToken;
                    existingToken.RefreshToken = refreshToken;
                    existingToken.LastUpdated = DateTime.UtcNow;
                    _logger.LogDebug("Update existing token record");
                }
                // Create new token record if none exists
                else
                {
                    var token = new AuthToken
                    {
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        LastUpdated = DateTime.UtcNow
                    };
                    dbContext.AuthTokens.Add(token);
                    _logger.LogDebug("Create new token record");
                }
                dbContext.SaveChanges();
                _logger.LogDebug($"Saved tokens to database: {accessToken}, {refreshToken}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tokens to database");
            }
        }
    }
}
