using ImageCompressionApi.Models;

namespace ImageCompressionApi.Services;

/// <summary>
/// Interface for image compression services
/// </summary>
public interface IImageCompressionService
{
    /// <summary>
    /// Compress an image file using FFmpeg
    /// </summary>
    /// <param name="file">The image file to compress</param>
    /// <param name="quality">Compression quality (1-100)</param>
    /// <param name="format">Output format (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compression result</returns>
    Task<ImageCompressionResult> CompressImageAsync(
        IFormFile file, 
        int quality, 
        string? format = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the compressed file by ID
    /// </summary>
    /// <param name="fileId">Unique file identifier</param>
    /// <returns>File stream and content type</returns>
    Task<(Stream FileStream, string ContentType, string FileName)?> GetCompressedFileAsync(string fileId);

    /// <summary>
    /// Check if FFmpeg is available and properly configured
    /// </summary>
    /// <returns>True if FFmpeg is available</returns>
    Task<bool> IsFFmpegAvailableAsync();

    /// <summary>
    /// Get FFmpeg version information
    /// </summary>
    /// <returns>Version string</returns>
    Task<string> GetFFmpegVersionAsync();

    /// <summary>
    /// Clean up expired temporary files
    /// </summary>
    /// <returns>Number of files cleaned up</returns>
    Task<int> CleanupExpiredFilesAsync();
} 