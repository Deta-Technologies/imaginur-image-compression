namespace ImageCompressionApi.Constants;

/// <summary>
/// Magic numbers (file signatures) for validating file types
/// </summary>
public static class FileMagicNumbers
{
    public static class Jpeg
    {
        public static readonly byte[] Header1 = new byte[] { 0xFF, 0xD8, 0xFF };
        public static readonly byte[] Header2 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        public static readonly byte[] Header3 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 };
        public static readonly byte[] Header4 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 };

        public static readonly byte[][] AllHeaders = new[]
        {
            Header1, Header2, Header3, Header4
        };
    }

    public static class Png
    {
        public static readonly byte[] Header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static readonly byte[][] AllHeaders = new[] { Header };
    }

    public static class WebP
    {
        /// <summary>
        /// RIFF header (first 4 bytes)
        /// </summary>
        public static readonly byte[] RiffHeader = new byte[] { 0x52, 0x49, 0x46, 0x46 };

        /// <summary>
        /// WEBP signature (bytes 8-11)
        /// </summary>
        public static readonly byte[] WebPSignature = new byte[] { 0x57, 0x45, 0x42, 0x50 };

        public static readonly byte[][] AllHeaders = new[] { RiffHeader };
    }

    public static class Bmp
    {
        public static readonly byte[] Header = new byte[] { 0x42, 0x4D }; // BM

        public static readonly byte[][] AllHeaders = new[] { Header };
    }

    /// <summary>
    /// Get all magic numbers mapped by MIME type
    /// </summary>
    public static readonly Dictionary<string, byte[][]> ByMimeType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = Jpeg.AllHeaders,
        ["image/png"] = Png.AllHeaders,
        ["image/webp"] = WebP.AllHeaders,
        ["image/bmp"] = Bmp.AllHeaders
    };
}
