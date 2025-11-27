using ImageCompressionApi.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ImageCompressionApi.Services;

/// <summary>
/// Orchestrates image compression operations by coordinating specialized services
/// </summary>
public class ImageCompressionService : IImageCompressionService, IDisposable
{
    private readonly ILogger<ImageCompressionService> _logger;
    private readonly IFileStorageService _fileStorage;
    private readonly IFFmpegExecutor _ffmpegExecutor;
    private readonly IImageFormatService _formatService;
    private readonly ImageCompressionSettings _settings;
    private readonly SemaphoreSlim _semaphore;
    private readonly string _tempDirectory;
    private static readonly Regex FileIdRegex = new(@"^[a-f0-9]{32}$", RegexOptions.Compiled);

    public ImageCompressionService(
        ILogger<ImageCompressionService> logger,
        IFileStorageService fileStorage,
        IFFmpegExecutor ffmpegExecutor,
        IImageFormatService formatService,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _fileStorage = fileStorage;
        _ffmpegExecutor = ffmpegExecutor;
        _formatService = formatService;
        _settings = settings.Value.ImageCompression;
        _semaphore = new SemaphoreSlim(
            _settings.MaxConcurrentOperations,
            _settings.MaxConcurrentOperations);

        _tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");
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

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Starting compression for file: {FileName}", file.FileName);

            // Generate unique file ID
            var fileId = Guid.NewGuid().ToString("N");
            var originalExtension = Path.GetExtension(file.FileName);

            // Save uploaded file
            var inputFileName = $"{fileId}_original{originalExtension}";
            var inputFilePath = Path.Combine(_tempDirectory, inputFileName);

            await using (var fileStream = new FileStream(inputFilePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream, cancellationToken);
            }

            var originalSize = await _fileStorage.GetFileSizeAsync(inputFilePath);
            _logger.LogInformation("Original file size: {Size} bytes", originalSize);

            // Determine output format
            var outputFormat = _formatService.DetermineOutputFormat(format, originalExtension);
            var outputExtension = _formatService.GetExtensionForFormat(outputFormat);
            var outputFileName = $"{fileId}_compressed{outputExtension}";
            var outputFilePath = Path.Combine(_tempDirectory, outputFileName);

            // Build FFmpeg arguments
            var ffmpegArgs = _formatService.BuildCompressionArguments(
                inputFilePath,
                outputFilePath,
                outputFormat,
                quality);

            // Execute compression
            var ffmpegResult = await _ffmpegExecutor.ExecuteAsync(
                ffmpegArgs,
                _settings.FFmpegTimeoutSeconds,
                cancellationToken);

            if (!ffmpegResult.IsSuccess)
            {
                throw new InvalidOperationException($"FFmpeg compression failed: {ffmpegResult.Error}");
            }

            // Get compressed file size
            var compressedSize = await _fileStorage.GetFileSizeAsync(outputFilePath);
            var compressionRatio = (1.0 - (double)compressedSize / originalSize) * 100;

            _logger.LogInformation(
                "Compression completed. Original: {Original} bytes, Compressed: {Compressed} bytes, Ratio: {Ratio:F1}%",
                originalSize, compressedSize, compressionRatio);

            // Clean up original file
            await _fileStorage.DeleteFileAsync(inputFilePath);

            stopwatch.Stop();

            return new ImageCompressionResult
            {
                OriginalSize = originalSize,
                CompressedSize = compressedSize,
                CompressionRatio = compressionRatio,
                Format = outputFormat,
                DownloadUrl = $"/api/image/download/{fileId}",
                FileId = fileId,
                CompressedAt = DateTime.UtcNow,
                Quality = quality,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image: {FileName}", file.FileName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Get the compressed file by ID
    /// </summary>
    public async Task<(Stream FileStream, string ContentType, string FileName)?> GetCompressedFileAsync(string fileId)
    {
        // Validate fileId format
        if (!IsValidFileId(fileId))
        {
            _logger.LogWarning("Invalid fileId format: {FileId}", fileId);
            return null;
        }

        var pattern = $"{fileId}_compressed.*";
        return await _fileStorage.GetFileAsync(pattern);
    }

    /// <summary>
    /// Check if FFmpeg is available
    /// </summary>
    public async Task<bool> IsFFmpegAvailableAsync()
    {
        return await _ffmpegExecutor.IsAvailableAsync();
    }

    /// <summary>
    /// Get FFmpeg version information
    /// </summary>
    public async Task<string> GetFFmpegVersionAsync()
    {
        return await _ffmpegExecutor.GetVersionAsync();
    }

    /// <summary>
    /// Clean up expired temporary files
    /// </summary>
    public async Task<int> CleanupExpiredFilesAsync()
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-_settings.TempFileRetentionMinutes);
            var files = await _fileStorage.GetFilesWithMetadataAsync(_tempDirectory);
            var deletedCount = 0;

            foreach (var file in files)
            {
                if (file.CreationTimeUtc < cutoffTime)
                {
                    await _fileStorage.DeleteFileAsync(file.FilePath);
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired temporary files", deletedCount);
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of expired files");
            return 0;
        }
    }

    /// <summary>
    /// Validate fileId format (SECURITY: Prevents path traversal attacks)
    /// </summary>
    /// <param name="fileId">The file ID to validate</param>
    /// <returns>True if valid (32 hex characters), false otherwise</returns>
    private static bool IsValidFileId(string fileId)
    {
        return !string.IsNullOrWhiteSpace(fileId) && FileIdRegex.IsMatch(fileId);
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
} 