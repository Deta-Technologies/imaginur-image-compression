namespace ImageCompressionApi.Models;

/// <summary>
/// Centralized provider for image format information and mappings
/// </summary>
public static class ImageFormatProvider
{
    private static readonly Dictionary<string, ImageFormatInfo> _formatsByName;
    private static readonly Dictionary<string, ImageFormatInfo> _formatsByExtension;
    private static readonly Dictionary<string, ImageFormatInfo> _formatsByMimeType;

    static ImageFormatProvider()
    {
        var formats = new[]
        {
            new ImageFormatInfo(
                name: "jpeg",
                extensions: new[] { ".jpg", ".jpeg" },
                mimeTypes: new[] { "image/jpeg", "image/jpg" },
                ffmpegCodec: "mjpeg",
                ffmpegFormat: "image2",
                supportsQuality: true,
                qualityParameter: "-q:v"
            ),
            new ImageFormatInfo(
                name: "png",
                extensions: new[] { ".png" },
                mimeTypes: new[] { "image/png" },
                ffmpegCodec: "png",
                ffmpegFormat: "image2",
                supportsQuality: false,
                defaultCompressionLevel: 9
            ),
            new ImageFormatInfo(
                name: "webp",
                extensions: new[] { ".webp" },
                mimeTypes: new[] { "image/webp" },
                ffmpegCodec: "libwebp",
                ffmpegFormat: "webp",
                supportsQuality: true,
                qualityParameter: "-q:v"
            ),
            new ImageFormatInfo(
                name: "bmp",
                extensions: new[] { ".bmp" },
                mimeTypes: new[] { "image/bmp", "image/x-bmp" },
                ffmpegCodec: "bmp",
                ffmpegFormat: "image2",
                supportsQuality: false
            )
        };

        _formatsByName = formats.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

        _formatsByExtension = formats
            .SelectMany(f => f.Extensions.Select(ext => new { Extension = ext, Format = f }))
            .ToDictionary(x => x.Extension, x => x.Format, StringComparer.OrdinalIgnoreCase);

        _formatsByMimeType = formats
            .SelectMany(f => f.MimeTypes.Select(mime => new { MimeType = mime, Format = f }))
            .ToDictionary(x => x.MimeType, x => x.Format, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get format information by format name (jpeg, png, webp, bmp)
    /// </summary>
    public static ImageFormatInfo? GetByName(string formatName)
    {
        if (string.IsNullOrWhiteSpace(formatName))
            return null;

        _formatsByName.TryGetValue(formatName, out var format);
        return format;
    }

    /// <summary>
    /// Get format information by file extension (.jpg, .png, etc.)
    /// </summary>
    public static ImageFormatInfo? GetByExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        // Ensure extension starts with dot
        if (!extension.StartsWith('.'))
            extension = $".{extension}";

        _formatsByExtension.TryGetValue(extension, out var format);
        return format;
    }

    /// <summary>
    /// Get format information by MIME type (image/jpeg, image/png, etc.)
    /// </summary>
    public static ImageFormatInfo? GetByMimeType(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return null;

        _formatsByMimeType.TryGetValue(mimeType, out var format);
        return format;
    }

    /// <summary>
    /// Get all supported MIME types
    /// </summary>
    public static IEnumerable<string> GetAllMimeTypes()
    {
        return _formatsByMimeType.Keys;
    }

    /// <summary>
    /// Get all supported extensions
    /// </summary>
    public static IEnumerable<string> GetAllExtensions()
    {
        return _formatsByExtension.Keys;
    }

    /// <summary>
    /// Get all supported format names
    /// </summary>
    public static IEnumerable<string> GetAllFormatNames()
    {
        return _formatsByName.Keys;
    }

    /// <summary>
    /// Check if a format name is valid
    /// </summary>
    public static bool IsValidFormatName(string formatName)
    {
        return !string.IsNullOrWhiteSpace(formatName) &&
               _formatsByName.ContainsKey(formatName);
    }

    /// <summary>
    /// Check if an extension is supported
    /// </summary>
    public static bool IsValidExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        if (!extension.StartsWith('.'))
            extension = $".{extension}";

        return _formatsByExtension.ContainsKey(extension);
    }

    /// <summary>
    /// Check if a MIME type is supported
    /// </summary>
    public static bool IsValidMimeType(string mimeType)
    {
        return !string.IsNullOrWhiteSpace(mimeType) &&
               _formatsByMimeType.ContainsKey(mimeType);
    }

    /// <summary>
    /// Determine output format based on user request or original extension
    /// </summary>
    public static ImageFormatInfo DetermineOutputFormat(string? requestedFormat, string originalExtension)
    {
        // If user specified a format and it's valid, use it
        if (!string.IsNullOrEmpty(requestedFormat) &&
            requestedFormat != "same" &&
            IsValidFormatName(requestedFormat))
        {
            return GetByName(requestedFormat)!;
        }

        // Otherwise, use the original format
        var format = GetByExtension(originalExtension);

        // Default to JPEG if format cannot be determined
        return format ?? GetByName("jpeg")!;
    }
}
