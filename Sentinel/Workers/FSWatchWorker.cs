using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sentinel.Workers
{
    public class FSWatchWorker : BackgroundService
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private readonly ILogger<FSWatchWorker> _logger;
        private readonly SentinelDbContext _dbContext;
        private readonly FSScanWorker _fsScanWorker;

        private readonly string _libraryFolder;

        public FSWatchWorker(ILogger<FSWatchWorker> logger, SentinelDbContext dbContext, FSScanWorker fSScanWorker, string libraryFolder)
        {
            _logger = logger;
            _dbContext = dbContext;
            _fsScanWorker = fSScanWorker;

            _libraryFolder = libraryFolder;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tcs = new TaskCompletionSource();

            _fsScanWorker.ExecuteAsync(stoppingToken);

            using (var watcher = new FileSystemWatcher(_libraryFolder))
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
            try
            {
                await _semaphore.WaitAsync(ct);
                throw new NotImplementedException();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task OnCreatedAsync(object sender, FileSystemEventArgs e, CancellationToken ct)
        {
            try
            {
                await _semaphore.WaitAsync(ct);
                throw new NotImplementedException();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task OnDeletedAsync(object sender, FileSystemEventArgs e, CancellationToken ct)
        {
            try
            {
                await _semaphore.WaitAsync(ct);
                var fileRemoved = e.FullPath;
                var appBinary = await _dbContext.AppBinaries
                    .Include(x => x.Files)
                    .ThenInclude(x => x.Chunks)
                    .SingleOrDefaultAsync(x => x.Files.Any(y => y.FilePath == fileRemoved), ct);
                appBinary?.Files.Remove(appBinary.Files.Single(x => x.FilePath == fileRemoved));
                if (appBinary?.Files.Count == 0)
                {
                    _dbContext.AppBinaries.Remove(appBinary);
                }
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "OnDeletedAsync Operation canceled");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task OnRenamedAsync(object sender, RenamedEventArgs e, CancellationToken ct)
        {
            try
            {
                await _semaphore.WaitAsync(ct);
                var fileChangedOldPath = e.OldFullPath;
                var fileChangedNewPath = e.FullPath;
                var appBinary = await _dbContext.AppBinaries
                    .Include(x => x.Files)
                    .ThenInclude(x => x.Chunks)
                    .SingleOrDefaultAsync(x => x.Files.Any(y => y.FilePath == fileChangedOldPath), ct);
                var file = appBinary?.Files.SingleOrDefault(x => x.FilePath == fileChangedOldPath);
                if (file != null)
                {
                    file.FilePath = fileChangedNewPath;
                    await _dbContext.SaveChangesAsync(ct);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "OnRenamedAsync Operation canceled");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
