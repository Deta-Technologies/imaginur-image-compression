namespace ImageCompressionApi.Models;

/// <summary>
/// Comprehensive information about a supported image format
/// </summary>
public record ImageFormatInfo
{
    public string Name { get; init; } = string.Empty;
    public string[] Extensions { get; init; } = Array.Empty<string>();
    public string[] MimeTypes { get; init; } = Array.Empty<string>();
    public string FFmpegCodec { get; init; } = string.Empty;
    public string FFmpegFormat { get; init; } = string.Empty;
    public bool SupportsQuality { get; init; }
    public string? QualityParameter { get; init; }
    public int? DefaultCompressionLevel { get; init; }

    public ImageFormatInfo() { }

    public ImageFormatInfo(
        string name,
        string[] extensions,
        string[] mimeTypes,
        string ffmpegCodec,
        string ffmpegFormat,
        bool supportsQuality,
        string? qualityParameter = null,
        int? defaultCompressionLevel = null)
    {
        Name = name;
        Extensions = extensions;
        MimeTypes = mimeTypes;
        FFmpegCodec = ffmpegCodec;
        FFmpegFormat = ffmpegFormat;
        SupportsQuality = supportsQuality;
        QualityParameter = qualityParameter;
        DefaultCompressionLevel = defaultCompressionLevel;
    }
}
