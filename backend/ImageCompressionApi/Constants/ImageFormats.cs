namespace ImageCompressionApi.Constants;

/// <summary>
/// Image format constants used throughout the application
/// </summary>
public static class ImageFormats
{
    // Format names
    public const string Jpeg = "jpeg";
    public const string Jpg = "jpg";
    public const string Png = "png";
    public const string WebP = "webp";
    public const string Bmp = "bmp";
    public const string Same = "same";

    /// <summary>
    /// All supported image formats
    /// </summary>
    public static readonly string[] AllFormats = { Jpeg, Jpg, Png, WebP, Bmp };

    /// <summary>
    /// Valid format options including "same"
    /// </summary>
    public static readonly string[] ValidFormatOptions = { Jpeg, Jpg, Png, WebP, Bmp, Same };
}

/// <summary>
/// MIME type constants for image formats
/// </summary>
public static class MimeTypes
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string WebP = "image/webp";
    public const string Bmp = "image/bmp";

    public static readonly string[] AllImageTypes = { Jpeg, Png, WebP, Bmp };
}

/// <summary>
/// File extension constants
/// </summary>
public static class FileExtensions
{
    public const string Jpg = ".jpg";
    public const string Jpeg = ".jpeg";
    public const string Png = ".png";
    public const string WebP = ".webp";
    public const string Bmp = ".bmp";
}

/// <summary>
/// Validation-related constants
/// </summary>
public static class ValidationConstants
{
    public const int MinQuality = 1;
    public const int MaxQuality = 100;
    public const int MagicNumberBufferSize = 16;
    public const int MinimumBytesRequired = 4;
    public const int WebPBufferSize = 12;
    public const int WebPMinimumBytes = 12;
}

/// <summary>
/// Magic number signatures for image format detection
/// </summary>
public static class MagicNumbers
{
    /// <summary>
    /// JPEG magic number: FF D8 FF
    /// </summary>
    public static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF };

    /// <summary>
    /// PNG magic number: 89 50 4E 47 0D 0A 1A 0A
    /// </summary>
    public static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// WebP magic number (RIFF): 52 49 46 46
    /// </summary>
    public static readonly byte[] WebPRiff = { 0x52, 0x49, 0x46, 0x46 };

    /// <summary>
    /// WebP identifier (WEBP): 57 45 42 50
    /// </summary>
    public static readonly byte[] WebPIdentifier = { 0x57, 0x45, 0x42, 0x50 };

    /// <summary>
    /// BMP magic number: 42 4D (BM)
    /// </summary>
    public static readonly byte[] Bmp = { 0x42, 0x4D };
}
