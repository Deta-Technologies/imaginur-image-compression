namespace ImageCompressionApi.Exceptions;

/// <summary>
/// Exception thrown when FFmpeg operations fail
/// </summary>
public class FFmpegException : Exception
{
    public int ExitCode { get; }
    public string FFmpegOutput { get; }
    public string FFmpegError { get; }

    public FFmpegException(string message, int exitCode, string output, string error)
        : base(message)
    {
        ExitCode = exitCode;
        FFmpegOutput = output;
        FFmpegError = error;
    }

    public FFmpegException(string message, int exitCode, string output, string error, Exception innerException)
        : base(message, innerException)
    {
        ExitCode = exitCode;
        FFmpegOutput = output;
        FFmpegError = error;
    }
}
