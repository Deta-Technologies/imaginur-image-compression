using System.ComponentModel.DataAnnotations;

namespace ImageCompressionApi.Models;

/// <summary>
/// Request model for image compression
/// </summary>
public class ImageCompressionRequest
{
    /// <summary>
    /// The image file to compress
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Compression quality (1-100, default: 80)
    /// </summary>
    [Range(1, 100)]
    public int Quality { get; set; } = 80;

    /// <summary>
    /// Output format (jpeg, png, webp, or same as input)
    /// </summary>
    public string? Format { get; set; }
}

/// <summary>
/// Validation attributes for image compression
/// </summary>
public class ImageCompressionValidation
{
    /// <summary>
    /// Validates file type and size
    /// </summary>
    public static class FileValidation
    {
        public static readonly string[] AllowedMimeTypes = 
        {
            "image/jpeg",
            "image/png", 
            "image/webp",
            "image/bmp"
        };

        public static readonly string[] AllowedExtensions = 
        {
            ".jpg", ".jpeg", ".png", ".webp", ".bmp"
        };

        public static readonly long MaxFileSize = 10485760; // 10MB
    }
} 