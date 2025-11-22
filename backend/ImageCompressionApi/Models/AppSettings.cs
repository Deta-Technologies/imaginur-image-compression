namespace ImageCompressionApi.Models;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppSettings
{
    public ImageCompressionSettings ImageCompression { get; set; } = new();
}

/// <summary>
/// Configuration settings for image compression
/// </summary>
public class ImageCompressionSettings
{
    /// <summary>
    /// Maximum file size in bytes (default: 10MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10485760; // 10MB

    /// <summary>
    /// Allowed image formats
    /// </summary>
    public List<string> AllowedFormats { get; set; } = new()
    {
        "jpeg", "jpg", "png", "webp", "bmp"
    };

    /// <summary>
    /// Default compression quality (1-100)
    /// </summary>
    public int DefaultQuality { get; set; } = 80;

    /// <summary>
    /// How long to retain temporary files (in minutes)
    /// </summary>
    public int TempFileRetentionMinutes { get; set; } = 30;

    /// <summary>
    /// Path to FFmpeg executable
    /// </summary>
    public string FFmpegPath { get; set; } = string.Empty;

    /// <summary>
    /// Maximum concurrent compression operations
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 5;

    /// <summary>
    /// Timeout for FFmpeg operations (in seconds)
    /// </summary>
    public int FFmpegTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Interval for running automatic cleanup of temporary files (in minutes)
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 10;
} 