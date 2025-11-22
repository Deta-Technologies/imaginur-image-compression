using ImageCompressionApi.Models;
using ImageCompressionApi.Services.FFmpeg;
using ImageCompressionApi.Services.Storage;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ImageCompressionApi.Services;

/// <summary>
/// Service for image compression orchestration using FFmpeg
/// </summary>
public class ImageCompressionService : IImageCompressionService
{
    private readonly ILogger<ImageCompressionService> _logger;
    private readonly IFFmpegProcessManager _ffmpegManager;
    private readonly ITemporaryFileManager _fileManager;
    private readonly ImageCompressionSettings _settings;
    private readonly SemaphoreSlim _semaphore;

    public ImageCompressionService(
        ILogger<ImageCompressionService> logger,
        IFFmpegProcessManager ffmpegManager,
        ITemporaryFileManager fileManager,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _ffmpegManager = ffmpegManager;
        _fileManager = fileManager;
        _settings = settings.Value.ImageCompression;
        _semaphore = new SemaphoreSlim(_settings.MaxConcurrentOperations, _settings.MaxConcurrentOperations);
    }

    /// <summary>
    /// Compress an image file using FFmpeg
    /// </summary>
    public async Task<ImageCompressionResult> CompressImageAsync(
        IFormFile file,
        int quality,
        string? format = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Wait for available slot
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                _logger.LogInformation("Starting compression for file: {FileName}", file.FileName);

                // Generate unique file ID
                var fileId = Guid.NewGuid().ToString("N");
                var originalExtension = Path.GetExtension(file.FileName);

                // Determine output format
                var formatInfo = ImageFormatProvider.DetermineOutputFormat(format, originalExtension);
                var outputExtension = formatInfo.Extensions[0];

                // Save uploaded file
                var inputFilePath = await _fileManager.SaveUploadedFileAsync(file, fileId, cancellationToken);
                var outputFilePath = inputFilePath.Replace("_original" + originalExtension, "_compressed" + outputExtension);

                var originalSize = _fileManager.GetFileInfo(inputFilePath).Length;
                _logger.LogInformation("Original file size: {Size} bytes", originalSize);

                // Compress using FFmpeg
                await _ffmpegManager.ExecuteCompressionAsync(inputFilePath, outputFilePath, formatInfo, quality, cancellationToken);

                // Get compressed file size
                var compressedSize = _fileManager.GetFileInfo(outputFilePath).Length;
                var compressionRatio = (1.0 - (double)compressedSize / originalSize) * 100;

                _logger.LogInformation("Compression completed. Original: {Original} bytes, Compressed: {Compressed} bytes, Ratio: {Ratio:F1}%",
                    originalSize, compressedSize, compressionRatio);

                // Clean up original file
                await _fileManager.DeleteFileAsync(inputFilePath);

                stopwatch.Stop();

                return new ImageCompressionResult
                {
                    OriginalSize = originalSize,
                    CompressedSize = compressedSize,
                    CompressionRatio = compressionRatio,
                    Format = formatInfo.Name,
                    DownloadUrl = $"/api/image/download/{fileId}",
                    FileId = fileId,
                    CompressedAt = DateTime.UtcNow,
                    Quality = quality,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image: {FileName}", file.FileName);
            throw;
        }
    }

    /// <summary>
    /// Get the compressed file by ID
    /// </summary>
    public async Task<(Stream FileStream, string ContentType, string FileName)?> GetCompressedFileAsync(string fileId)
    {
        return await _fileManager.GetCompressedFileAsync(fileId);
    }

    /// <summary>
    /// Check if FFmpeg is available
    /// </summary>
    public async Task<bool> IsFFmpegAvailableAsync()
    {
        return await _ffmpegManager.IsFFmpegAvailableAsync();
    }

    /// <summary>
    /// Get FFmpeg version information
    /// </summary>
    public async Task<string> GetFFmpegVersionAsync()
    {
        return await _ffmpegManager.GetFFmpegVersionAsync();
    }

    /// <summary>
    /// Clean up expired temporary files
    /// </summary>
    public async Task<int> CleanupExpiredFilesAsync()
    {
        var retentionTime = TimeSpan.FromMinutes(_settings.TempFileRetentionMinutes);
        return await _fileManager.CleanupExpiredFilesAsync(retentionTime);
    }
}
