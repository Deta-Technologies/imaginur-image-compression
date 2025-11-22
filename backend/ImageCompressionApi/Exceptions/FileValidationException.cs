namespace ImageCompressionApi.Exceptions;

/// <summary>
/// Exception thrown when file validation fails
/// </summary>
public class FileValidationException : Exception
{
    public string ErrorCode { get; }
    public string ValidationError { get; }

    public FileValidationException(string message, string errorCode, string validationError)
        : base(message)
    {
        ErrorCode = errorCode;
        ValidationError = validationError;
    }

    public FileValidationException(string message, string errorCode, string validationError, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ValidationError = validationError;
    }
}
