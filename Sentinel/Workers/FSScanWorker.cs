using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentinel.Plugin.Configs;
using Sentinel.Plugin.Contracts;
using Sentinel.Plugin.Models;

namespace Sentinel.Workers
{
    public class FSScanWorker : BackgroundService
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private readonly ILogger<FSScanWorker> _logger;
        private readonly SentinelDbContext _dbContext;

        private bool _configured;
        private IPlugin _plugin = null!;
        private PluginConfigBase _pluginConfig = null!;

        private IQueryable<AppBinary> _appBinaries = null!;

        public FSScanWorker(ILogger<FSScanWorker> logger, SentinelDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configured = false;
        }

        public void Configure(IPlugin plugin, PluginConfigBase config)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _pluginConfig = config ?? throw new ArgumentNullException(nameof(config));
            _plugin.Config = _pluginConfig;
            _configured = true;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configured)
            {
                throw new InvalidOperationException("FSScanWorker is not configured.");
            }

            _appBinaries = _dbContext.AppBinaries
                .Include(x => x.Files)
                .ThenInclude(x => x.Chunks)
                .Select(x => x.ToPluginModel());

            var tcs = new TaskCompletionSource();

            using (var watcher = new FileSystemWatcher(_pluginConfig.LibraryFolder))
            {
                watcher.NotifyFilter = NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.Size;

                watcher.Changed += async (sender, e) => await OnChangedAsync(sender, e, stoppingToken);
                watcher.Created += async (sender, e) => await OnCreatedAsync(sender, e, stoppingToken);
                watcher.Deleted += async (sender, e) => await OnDeletedAsync(sender, e, stoppingToken);
                watcher.Renamed += async (sender, e) => await OnRenamedAsync(sender, e, stoppingToken);
                watcher.Error += (sender, e) => _logger.LogError(e.GetException(), "Error in FileSystemWatcher");

                watcher.Filter = "*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                stoppingToken.Register(() =>
                {
                    watcher.EnableRaisingEvents = false;
                    tcs.SetResult();
                });
            }

            return tcs.Task;
        }

        private async Task OnChangedAsync(object sender, FileSystemEventArgs e, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                throw new NotImplementedException();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task OnCreatedAsync(object sender, FileSystemEventArgs e, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                throw new NotImplementedException();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task OnDeletedAsync(object sender, FileSystemEventArgs e, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                var fileRemoved = e.FullPath;
                var appBinaryToRemove = ;
                if (appBinaryToRemove != null)
                {
                    _dbContext.AppBinaries.Remove(appBinaryToRemove);
                    await _dbContext.SaveChangesAsync(ct);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task OnRenamedAsync(object sender, RenamedEventArgs e, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                throw new NotImplementedException();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
