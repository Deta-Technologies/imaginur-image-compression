namespace ImageCompressionApi.Models;

/// <summary>
/// Base API response model
/// </summary>
/// <typeparam name="T">Type of data returned</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Data returned from the operation
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error information if operation failed
    /// </summary>
    public ApiError? Error { get; set; }

    /// <summary>
    /// Create a successful response
    /// </summary>
    public static ApiResponse<T> CreateSuccess(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    public static ApiResponse<T> CreateError(string message, string code = "UNKNOWN_ERROR")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ApiError
            {
                Message = message,
                Code = code
            }
        };
    }
}

/// <summary>
/// API error information
/// </summary>
public class ApiError
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Image compression result data
/// </summary>
public class ImageCompressionResult
{
    /// <summary>
    /// Original file size in bytes
    /// </summary>
    public long OriginalSize { get; set; }

    /// <summary>
    /// Compressed file size in bytes
    /// </summary>
    public long CompressedSize { get; set; }

    /// <summary>
    /// Compression ratio as percentage
    /// </summary>
    public double CompressionRatio { get; set; }

    /// <summary>
    /// Output format of the compressed image
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// URL to download the compressed image
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for the compressed file
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the compression was completed
    /// </summary>
    public DateTime CompressedAt { get; set; }

    /// <summary>
    /// Quality level used for compression
    /// </summary>
    public int Quality { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Common error codes
/// </summary>
public static class ErrorCodes
{
    public const string INVALID_FILE_FORMAT = "INVALID_FILE_FORMAT";
    public const string FILE_TOO_LARGE = "FILE_TOO_LARGE";
    public const string COMPRESSION_FAILED = "COMPRESSION_FAILED";
    public const string FFMPEG_NOT_FOUND = "FFMPEG_NOT_FOUND";
    public const string FFMPEG_ERROR = "FFMPEG_ERROR";
    public const string FILE_NOT_FOUND = "FILE_NOT_FOUND";
    public const string INVALID_PARAMETERS = "INVALID_PARAMETERS";
    public const string PROCESSING_TIMEOUT = "PROCESSING_TIMEOUT";
    public const string DISK_SPACE_ERROR = "DISK_SPACE_ERROR";
    public const string UNKNOWN_ERROR = "UNKNOWN_ERROR";
} 