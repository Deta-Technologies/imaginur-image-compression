using ImageCompressionApi.Services;
using ImageCompressionApi.Models;
using Microsoft.Extensions.Options;

namespace ImageCompressionApi.BackgroundServices
{
    /// <summary>
    /// Background service for cleaning up expired files
    /// </summary>
    public class CleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval;

        public CleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CleanupBackgroundService> logger,
            IOptions<AppSettings> settings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _cleanupInterval = TimeSpan.FromMinutes(settings.Value.ImageCompression.CleanupIntervalMinutes);
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