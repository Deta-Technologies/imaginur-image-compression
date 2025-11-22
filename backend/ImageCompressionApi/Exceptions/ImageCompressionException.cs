namespace ImageCompressionApi.Exceptions;

/// <summary>
/// Exception thrown when image compression operations fail
/// </summary>
public class ImageCompressionException : Exception
{
    public string ErrorCode { get; }

    public ImageCompressionException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public ImageCompressionException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
