namespace ImageCompressionApi.Services;

/// <summary>
/// Abstraction for FFmpeg process execution
/// </summary>
public interface IFFmpegExecutor
{
    /// <summary>
    /// Execute FFmpeg with given arguments
    /// </summary>
    Task<FFmpegResult> ExecuteAsync(
        IEnumerable<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if FFmpeg is available
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get FFmpeg version
    /// </summary>
    Task<string> GetVersionAsync();
}

/// <summary>
/// Result of FFmpeg execution
/// </summary>
public record FFmpegResult(
    int ExitCode,
    string Output,
    string Error,
    bool IsSuccess,
    TimeSpan Duration);
