namespace ImageCompressionApi.Models;

/// <summary>
/// Compression statistics
/// </summary>
public class CompressionStats
{
    public long MaxFileSize { get; set; }
    public List<string> SupportedFormats { get; set; } = new();
    public int DefaultQuality { get; set; }
    public int MaxConcurrentOperations { get; set; }
    public int TempFileRetentionMinutes { get; set; }
    public int FFmpegTimeoutSeconds { get; set; }
}
