using ImageCompressionApi.Services;

namespace ImageCompressionApi.BackgroundServices
{
    /// <summary>
    /// Background service for cleaning up expired files
    /// </summary>
    public class CleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);

        public CleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cleanup background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var compressionService = scope.ServiceProvider.GetRequiredService<IImageCompressionService>();
                
                    var deletedCount = await compressionService.CleanupExpiredFilesAsync();
                    if (deletedCount > 0)
                    {
                        _logger.LogInformation("Background cleanup completed. Deleted {Count} files", deletedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during background cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Cleanup background service stopped");
        }
    }
}