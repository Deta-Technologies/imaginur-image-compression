using ImageCompressionApi.Models;

namespace ImageCompressionApi.Services.FFmpeg;

/// <summary>
/// Manages FFmpeg process execution for image compression
/// </summary>
public interface IFFmpegProcessManager
{
    /// <summary>
    /// Execute FFmpeg compression with the specified parameters
    /// </summary>
    Task ExecuteCompressionAsync(
        string inputPath,
        string outputPath,
        ImageFormatInfo format,
        int quality,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if FFmpeg is available on the system
    /// </summary>
    Task<bool> IsFFmpegAvailableAsync();

    /// <summary>
    /// Get the version of FFmpeg installed
    /// </summary>
    Task<string> GetFFmpegVersionAsync();
}
