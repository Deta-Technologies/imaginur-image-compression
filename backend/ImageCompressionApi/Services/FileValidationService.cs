using ImageCompressionApi.Constants;
using ImageCompressionApi.Models;
using Microsoft.Extensions.Options;

namespace ImageCompressionApi.Services;

/// <summary>
/// Service for validating uploaded files
/// </summary>
public class FileValidationService
{
    private readonly ILogger<FileValidationService> _logger;
    private readonly ImageCompressionSettings _settings;

    // Magic numbers for file type detection
    private static readonly Dictionary<string, byte[][]> FileMagicNumbers = new()
    {
        [MimeTypes.Jpeg] = new[]
        {
            MagicNumbers.Jpeg,
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }
        },
        [MimeTypes.Png] = new[]
        {
            MagicNumbers.Png
        },
        [MimeTypes.WebP] = new[]
        {
            MagicNumbers.WebPRiff // RIFF header (WebP specific check done later)
        },
        [MimeTypes.Bmp] = new[]
        {
            MagicNumbers.Bmp
        }
    };

    public FileValidationService(
        ILogger<FileValidationService> logger,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value.ImageCompression;
    }

    /// <summary>
    /// Validate an uploaded file
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <returns>Validation result</returns>
    public async Task<FileValidationResult> ValidateFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return FileValidationResult.Fail("No file provided or file is empty", ErrorCodes.INVALID_PARAMETERS);
        }

        // Check file size
        if (file.Length > _settings.MaxFileSizeBytes)
        {
            return FileValidationResult.Fail(
                $"File size ({FormatFileSize(file.Length)}) exceeds maximum allowed size ({FormatFileSize(_settings.MaxFileSizeBytes)})",
                ErrorCodes.FILE_TOO_LARGE);
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !IsAllowedExtension(extension))
        {
            return FileValidationResult.Fail(
                $"File extension '{extension}' is not allowed. Supported formats: {string.Join(", ", _settings.AllowedFormats)}",
                ErrorCodes.INVALID_FILE_FORMAT);
        }

        // Check MIME type
        if (!IsAllowedMimeType(file.ContentType))
        {
            return FileValidationResult.Fail(
                $"MIME type '{file.ContentType}' is not allowed",
                ErrorCodes.INVALID_FILE_FORMAT);
        }

        // Check file magic numbers
        var magicNumberResult = await ValidateFileMagicNumbersAsync(file);
        if (!magicNumberResult.IsValid)
        {
            return magicNumberResult;
        }

        // Additional WebP validation
        if (file.ContentType == MimeTypes.WebP)
        {
            var webpResult = await ValidateWebPFileAsync(file);
            if (!webpResult.IsValid)
            {
                return webpResult;
            }
        }

        _logger.LogDebug("File validation passed for: {FileName}", file.FileName);
        return FileValidationResult.Success();
    }

    /// <summary>
    /// Validate file using magic numbers
    /// </summary>
    private async Task<FileValidationResult> ValidateFileMagicNumbersAsync(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[ValidationConstants.MagicNumberBufferSize];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead < ValidationConstants.MinimumBytesRequired)
            {
                return FileValidationResult.Fail("File is too small to determine type", ErrorCodes.INVALID_FILE_FORMAT);
            }

            var detectedMimeType = DetectMimeTypeFromMagicNumber(buffer);
            if (detectedMimeType == null)
            {
                return FileValidationResult.Fail("Could not determine file type from content", ErrorCodes.INVALID_FILE_FORMAT);
            }

            if (!IsAllowedMimeType(detectedMimeType))
            {
                return FileValidationResult.Fail(
                    $"File content indicates type '{detectedMimeType}' which is not allowed",
                    ErrorCodes.INVALID_FILE_FORMAT);
            }

            // Check if detected type matches declared type
            if (!string.IsNullOrEmpty(file.ContentType) && 
                !IsMimeTypeCompatible(file.ContentType, detectedMimeType))
            {
                _logger.LogWarning("MIME type mismatch for file {FileName}: declared={DeclaredType}, detected={DetectedType}",
                    file.FileName, file.ContentType, detectedMimeType);
                
                return FileValidationResult.Fail(
                    $"File content type mismatch. Declared: {file.ContentType}, Detected: {detectedMimeType}",
                    ErrorCodes.INVALID_FILE_FORMAT);
            }

            return FileValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file magic numbers for: {FileName}", file.FileName);
            return FileValidationResult.Fail("Error validating file content", ErrorCodes.UNKNOWN_ERROR);
        }
    }

    /// <summary>
    /// Additional WebP file validation
    /// </summary>
    private async Task<FileValidationResult> ValidateWebPFileAsync(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[ValidationConstants.WebPBufferSize];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead < ValidationConstants.WebPMinimumBytes)
            {
                return FileValidationResult.Fail("WebP file is too small", ErrorCodes.INVALID_FILE_FORMAT);
            }

            // Check RIFF header (matches MagicNumbers.WebPRiff)
            if (!buffer.Take(MagicNumbers.WebPRiff.Length).SequenceEqual(MagicNumbers.WebPRiff))
            {
                return FileValidationResult.Fail("Invalid WebP file: missing RIFF header", ErrorCodes.INVALID_FILE_FORMAT);
            }

            // Check WEBP signature at offset 8 (matches MagicNumbers.WebPIdentifier)
            if (!buffer.Skip(8).Take(MagicNumbers.WebPIdentifier.Length).SequenceEqual(MagicNumbers.WebPIdentifier))
            {
                return FileValidationResult.Fail("Invalid WebP file: missing WEBP signature", ErrorCodes.INVALID_FILE_FORMAT);
            }

            return FileValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating WebP file: {FileName}", file.FileName);
            return FileValidationResult.Fail("Error validating WebP file", ErrorCodes.UNKNOWN_ERROR);
        }
    }

    /// <summary>
    /// Detect MIME type from magic number
    /// </summary>
    private string? DetectMimeTypeFromMagicNumber(byte[] buffer)
    {
        foreach (var kvp in FileMagicNumbers)
        {
            var mimeType = kvp.Key;
            var magicNumbers = kvp.Value;

            foreach (var magicNumber in magicNumbers)
            {
                if (buffer.Length >= magicNumber.Length && 
                    buffer.Take(magicNumber.Length).SequenceEqual(magicNumber))
                {
                    return mimeType;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Check if extension is allowed
    /// </summary>
    private bool IsAllowedExtension(string extension)
    {
        var allowedExtensions = _settings.AllowedFormats.SelectMany(format => GetExtensionsForFormat(format));
        return allowedExtensions.Contains(extension);
    }

    /// <summary>
    /// Check if MIME type is allowed
    /// </summary>
    private bool IsAllowedMimeType(string? mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
            return false;

        return MimeTypes.AllImageTypes.Contains(mimeType.ToLowerInvariant());
    }

    /// <summary>
    /// Check if two MIME types are compatible
    /// </summary>
    private bool IsMimeTypeCompatible(string declaredType, string detectedType)
    {
        // Exact match
        if (declaredType.Equals(detectedType, StringComparison.OrdinalIgnoreCase))
            return true;

        // Handle JPEG variations (image/jpeg and image/jpg are equivalent)
        if ((declaredType == MimeTypes.Jpeg || declaredType == "image/jpg") &&
            (detectedType == MimeTypes.Jpeg || detectedType == "image/jpg"))
            return true;

        return false;
    }

    /// <summary>
    /// Get file extensions for a format
    /// </summary>
    private string[] GetExtensionsForFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            ImageFormats.Jpeg or ImageFormats.Jpg => new[] { FileExtensions.Jpg, FileExtensions.Jpeg },
            ImageFormats.Png => new[] { FileExtensions.Png },
            ImageFormats.WebP => new[] { FileExtensions.WebP },
            ImageFormats.Bmp => new[] { FileExtensions.Bmp },
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Format file size for display
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB" };
        var order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// File validation result
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public static FileValidationResult Success() => new() { IsValid = true };
    
    public static FileValidationResult Fail(string message, string code) => new() 
    { 
        IsValid = false, 
        ErrorMessage = message, 
        ErrorCode = code 
    };
} 