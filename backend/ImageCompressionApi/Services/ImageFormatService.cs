using ImageCompressionApi.Constants;
using ImageCompressionApi.Models;
using Microsoft.Extensions.Options;

namespace ImageCompressionApi.Services;

/// <summary>
/// Image format service with centralized format handling
/// </summary>
public class ImageFormatService : IImageFormatService
{
    private readonly ILogger<ImageFormatService> _logger;
    private readonly ImageCompressionSettings _settings;

    // Format mapping registry
    private static readonly Dictionary<string, FormatInfo> FormatRegistry = new()
    {
        [ImageFormats.Jpeg] = new FormatInfo(
            FileExtensions.Jpg,
            MimeTypes.Jpeg,
            new[] { "-f", "image2", "-vcodec", "mjpeg", "-q:v" },
            SupportsQuality: true),

        [ImageFormats.Jpg] = new FormatInfo(
            FileExtensions.Jpg,
            MimeTypes.Jpeg,
            new[] { "-f", "image2", "-vcodec", "mjpeg", "-q:v" },
            SupportsQuality: true),

        [ImageFormats.Png] = new FormatInfo(
            FileExtensions.Png,
            MimeTypes.Png,
            new[] { "-f", "image2", "-vcodec", "png", "-compression_level", "9" },
            SupportsQuality: false),

        [ImageFormats.WebP] = new FormatInfo(
            FileExtensions.WebP,
            MimeTypes.WebP,
            new[] { "-f", "webp", "-c:v", "libwebp", "-q:v" },
            SupportsQuality: true),

        [ImageFormats.Bmp] = new FormatInfo(
            FileExtensions.Bmp,
            MimeTypes.Bmp,
            new[] { "-f", "image2", "-vcodec", "bmp" },
            SupportsQuality: false)
    };

    public ImageFormatService(
        ILogger<ImageFormatService> logger,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value.ImageCompression;
    }

    public string DetermineOutputFormat(string? requestedFormat, string originalExtension)
    {
        // If format explicitly requested and not "same", use it
        if (!string.IsNullOrEmpty(requestedFormat) &&
            !requestedFormat.Equals(ImageFormats.Same, StringComparison.OrdinalIgnoreCase))
        {
            var normalized = requestedFormat.ToLowerInvariant();

            if (!IsFormatSupported(normalized))
            {
                _logger.LogWarning("Unsupported format requested: {Format}, falling back to JPEG", requestedFormat);
                return ImageFormats.Jpeg;
            }

            return normalized;
        }

        // Determine from original extension
        var format = originalExtension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageFormats.Jpeg,
            ".png" => ImageFormats.Png,
            ".webp" => ImageFormats.WebP,
            ".bmp" => ImageFormats.Bmp,
            _ => ImageFormats.Jpeg  // Default fallback
        };

        _logger.LogDebug("Determined output format: {Format} from extension: {Extension}",
            format, originalExtension);

        return format;
    }

    public string GetExtensionForFormat(string format)
    {
        var normalizedFormat = format.ToLowerInvariant();

        if (FormatRegistry.TryGetValue(normalizedFormat, out var formatInfo))
        {
            return formatInfo.Extension;
        }

        _logger.LogWarning("Unknown format: {Format}, returning .jpg", format);
        return FileExtensions.Jpg;
    }

    public string GetMimeTypeForFormat(string format)
    {
        var normalizedFormat = format.ToLowerInvariant();

        if (FormatRegistry.TryGetValue(normalizedFormat, out var formatInfo))
        {
            return formatInfo.MimeType;
        }

        return "application/octet-stream";
    }

    public string GetMimeTypeForExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => MimeTypes.Jpeg,
            ".png" => MimeTypes.Png,
            ".webp" => MimeTypes.WebP,
            ".bmp" => MimeTypes.Bmp,
            _ => "application/octet-stream"
        };
    }

    public IEnumerable<string> BuildCompressionArguments(
        string inputPath,
        string outputPath,
        string format,
        int quality)
    {
        var args = new List<string>
        {
            "-i",
            inputPath,
            "-y"  // Overwrite output
        };

        var normalizedFormat = format.ToLowerInvariant();

        if (!FormatRegistry.TryGetValue(normalizedFormat, out var formatInfo))
        {
            throw new ArgumentException($"Unsupported format: {format}");
        }

        // Add format-specific arguments
        args.AddRange(formatInfo.FFmpegArgs);

        // Add quality parameter for formats that support it
        if (formatInfo.SupportsQuality)
        {
            args.Add(quality.ToString());
        }

        // Add output path
        args.Add(outputPath);

        _logger.LogDebug("Built FFmpeg arguments for format {Format}: {ArgCount} args",
            format, args.Count);

        return args;
    }

    public bool IsFormatSupported(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return false;

        var normalizedFormat = format.ToLowerInvariant();
        return FormatRegistry.ContainsKey(normalizedFormat) &&
               _settings.AllowedFormats.Contains(normalizedFormat);
    }

    private record FormatInfo(
        string Extension,
        string MimeType,
        string[] FFmpegArgs,
        bool SupportsQuality = true);
}
