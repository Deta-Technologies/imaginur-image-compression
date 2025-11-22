namespace ImageCompressionApi.Models;

/// <summary>
/// System health status
/// </summary>
public class HealthStatus
{
    public bool IsHealthy { get; set; }
    public bool FFmpegAvailable { get; set; }
    public string FFmpegVersion { get; set; } = string.Empty;
    public long MaxFileSize { get; set; }
    public List<string> SupportedFormats { get; set; } = new();
    public int DefaultQuality { get; set; }
    public DateTime Timestamp { get; set; }
}
