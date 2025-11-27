namespace ImageCompressionApi.Services;

/// <summary>
/// Service for image format operations
/// </summary>
public interface IImageFormatService
{
    /// <summary>
    /// Determine output format based on request and original file
    /// </summary>
    string DetermineOutputFormat(string? requestedFormat, string originalExtension);

    /// <summary>
    /// Get file extension for format
    /// </summary>
    string GetExtensionForFormat(string format);

    /// <summary>
    /// Get MIME type for format
    /// </summary>
    string GetMimeTypeForFormat(string format);

    /// <summary>
    /// Get MIME type for file extension
    /// </summary>
    string GetMimeTypeForExtension(string extension);

    /// <summary>
    /// Build FFmpeg compression arguments for format
    /// </summary>
    IEnumerable<string> BuildCompressionArguments(
        string inputPath,
        string outputPath,
        string format,
        int quality);

    /// <summary>
    /// Validate format is supported
    /// </summary>
    bool IsFormatSupported(string format);
}
