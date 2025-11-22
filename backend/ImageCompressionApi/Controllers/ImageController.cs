using ImageCompressionApi.Models;
using ImageCompressionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace ImageCompressionApi.Controllers;

/// <summary>
/// API controller for image compression operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ImageController : ControllerBase
{
    private readonly ILogger<ImageController> _logger;
    private readonly IImageCompressionService _compressionService;
    private readonly FileValidationService _validationService;
    private readonly ImageCompressionSettings _settings;

    public ImageController(
        ILogger<ImageController> logger,
        IImageCompressionService compressionService,
        FileValidationService validationService,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _compressionService = compressionService;
        _validationService = validationService;
        _settings = settings.Value.ImageCompression;
    }

    /// <summary>
    /// Compress an image file
    /// </summary>
    /// <param name="file">The image file to compress</param>
    /// <param name="quality">Compression quality (1-100, default: 80)</param>
    /// <param name="format">Output format (jpeg, png, webp, or same as input)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compression result with download URL</returns>
    /// <response code="200">Image compressed successfully</response>
    /// <response code="400">Invalid request parameters or file</response>
    /// <response code="413">File too large</response>
    /// <response code="415">Unsupported media type</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("compress")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ImageCompressionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 413)]
    [ProducesResponseType(typeof(ApiResponse<object>), 415)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CompressImage(
        [FromForm] IFormFile file,
        [FromForm] [Range(1, 100)] int quality = 80,
        [FromForm] string? format = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Received compression request for file: {FileName}, Quality: {Quality}, Format: {Format}", 
                file?.FileName, quality, format);

            // Validate file
            var validationResult = await _validationService.ValidateFileAsync(file);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("File validation failed: {Error}", validationResult.ErrorMessage);
                return BadRequest(ApiResponse<object>.CreateError(validationResult.ErrorMessage!, validationResult.ErrorCode!));
            }

            // Validate format parameter
            if (!string.IsNullOrEmpty(format) && !IsValidFormat(format))
            {
                return BadRequest(ApiResponse<object>.CreateError(
                    $"Invalid format '{format}'. Supported formats: {string.Join(", ", _settings.AllowedFormats)}",
                    ErrorCodes.INVALID_PARAMETERS));
            }

            // Compress the image
            var result = await _compressionService.CompressImageAsync(file, quality, format, cancellationToken);

            _logger.LogInformation("Image compression completed successfully. FileId: {FileId}, Original: {OriginalSize}, Compressed: {CompressedSize}", 
                result.FileId, result.OriginalSize, result.CompressedSize);

            return Ok(ApiResponse<ImageCompressionResult>.CreateSuccess(result));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Image compression was cancelled");
            return StatusCode(499, ApiResponse<object>.CreateError("Operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Image compression timed out");
            return StatusCode(408, ApiResponse<object>.CreateError("Compression operation timed out", ErrorCodes.PROCESSING_TIMEOUT));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("FFmpeg"))
        {
            _logger.LogError(ex, "FFmpeg error during compression");
            return StatusCode(500, ApiResponse<object>.CreateError("Image compression failed", ErrorCodes.FFMPEG_ERROR));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image compression");
            return StatusCode(500, ApiResponse<object>.CreateError("An unexpected error occurred", ErrorCodes.UNKNOWN_ERROR));
        }
    }

    /// <summary>
    /// Download a compressed image file
    /// </summary>
    /// <param name="fileId">The unique file identifier</param>
    /// <returns>The compressed image file</returns>
    /// <response code="200">File downloaded successfully</response>
    /// <response code="404">File not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("download/{fileId}")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> DownloadCompressedImage(string fileId)
    {
        try
        {
            if (string.IsNullOrEmpty(fileId))
            {
                return BadRequest(ApiResponse<object>.CreateError("File ID is required", ErrorCodes.INVALID_PARAMETERS));
            }

            _logger.LogInformation("Download requested for file: {FileId}", fileId);

            var fileResult = await _compressionService.GetCompressedFileAsync(fileId);
            if (fileResult == null)
            {
                _logger.LogWarning("File not found: {FileId}", fileId);
                return NotFound(ApiResponse<object>.CreateError("File not found", ErrorCodes.FILE_NOT_FOUND));
            }

            var (fileStream, contentType, fileName) = fileResult.Value;

            _logger.LogInformation("File download starting: {FileId}, FileName: {FileName}", fileId, fileName);

            return File(fileStream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FileId}", fileId);
            return StatusCode(500, ApiResponse<object>.CreateError("Error downloading file", ErrorCodes.UNKNOWN_ERROR));
        }
    }

    /// <summary>
    /// Get system health and FFmpeg status
    /// </summary>
    /// <returns>System health information</returns>
    /// <response code="200">System health retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<HealthStatus>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var isFFmpegAvailable = await _compressionService.IsFFmpegAvailableAsync();
            var ffmpegVersion = await _compressionService.GetFFmpegVersionAsync();

            var healthStatus = new HealthStatus
            {
                IsHealthy = isFFmpegAvailable,
                FFmpegAvailable = isFFmpegAvailable,
                FFmpegVersion = ffmpegVersion,
                MaxFileSize = _settings.MaxFileSizeBytes,
                SupportedFormats = _settings.AllowedFormats,
                DefaultQuality = _settings.DefaultQuality,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<HealthStatus>.CreateSuccess(healthStatus));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health status");
            return StatusCode(500, ApiResponse<object>.CreateError("Error getting health status", ErrorCodes.UNKNOWN_ERROR));
        }
    }

    /// <summary>
    /// Clean up expired temporary files
    /// </summary>
    /// <returns>Cleanup result</returns>
    /// <response code="200">Cleanup completed successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(ApiResponse<CleanupResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> CleanupExpiredFiles()
    {
        try
        {
            _logger.LogInformation("Manual cleanup requested");

            var deletedCount = await _compressionService.CleanupExpiredFilesAsync();

            var result = new CleanupResult
            {
                DeletedCount = deletedCount,
                CleanupTime = DateTime.UtcNow
            };

            _logger.LogInformation("Cleanup completed. Deleted {Count} files", deletedCount);

            return Ok(ApiResponse<CleanupResult>.CreateSuccess(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
            return StatusCode(500, ApiResponse<object>.CreateError("Error during cleanup", ErrorCodes.UNKNOWN_ERROR));
        }
    }

    /// <summary>
    /// Get compression statistics
    /// </summary>
    /// <returns>Usage statistics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<CompressionStats>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public IActionResult GetStatistics()
    {
        try
        {
            var stats = new CompressionStats
            {
                MaxFileSize = _settings.MaxFileSizeBytes,
                SupportedFormats = _settings.AllowedFormats,
                DefaultQuality = _settings.DefaultQuality,
                MaxConcurrentOperations = _settings.MaxConcurrentOperations,
                TempFileRetentionMinutes = _settings.TempFileRetentionMinutes,
                FFmpegTimeoutSeconds = _settings.FFmpegTimeoutSeconds
            };

            return Ok(ApiResponse<CompressionStats>.CreateSuccess(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, ApiResponse<object>.CreateError("Error getting statistics", ErrorCodes.UNKNOWN_ERROR));
        }
    }

    /// <summary>
    /// Validate if the requested format is supported
    /// </summary>
    private bool IsValidFormat(string format)
    {
        if (format.Equals("same", StringComparison.OrdinalIgnoreCase))
            return true;

        return ImageFormatProvider.IsValidFormatName(format);
    }
} 