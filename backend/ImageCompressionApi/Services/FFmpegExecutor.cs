using ImageCompressionApi.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ImageCompressionApi.Services;

/// <summary>
/// FFmpeg process executor
/// </summary>
public class FFmpegExecutor : IFFmpegExecutor
{
    private readonly ILogger<FFmpegExecutor> _logger;
    private readonly string _ffmpegPath;

    public FFmpegExecutor(
        ILogger<FFmpegExecutor> logger,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        var ffmpegPathConfig = settings.Value.ImageCompression.FFmpegPath;

        _ffmpegPath = string.IsNullOrEmpty(ffmpegPathConfig)
            ? GetSystemFFmpegPath()
            : ffmpegPathConfig;
    }

    public async Task<FFmpegResult> ExecuteAsync(
        IEnumerable<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Executing FFmpeg: {Path} with {ArgCount} arguments",
            _ffmpegPath, arguments.Count());

        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Add arguments safely using ArgumentList
        foreach (var arg in arguments)
        {
            processInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = processInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait with timeout
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("FFmpeg execution timed out after {Timeout}s, killing process", timeoutSeconds);
            process.Kill();
            throw new TimeoutException($"FFmpeg operation timed out after {timeoutSeconds} seconds");
        }

        stopwatch.Stop();

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();
        var exitCode = process.ExitCode;
        var isSuccess = exitCode == 0;

        if (!isSuccess)
        {
            _logger.LogError("FFmpeg failed with exit code {ExitCode}: {Error}", exitCode, error);
        }
        else
        {
            _logger.LogDebug("FFmpeg completed successfully in {Duration}ms", stopwatch.ElapsedMilliseconds);
        }

        return new FFmpegResult(exitCode, output, error, isSuccess, stopwatch.Elapsed);
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var result = await ExecuteVersionCommandAsync();
            return result.IsSuccess && result.Output.Contains("ffmpeg version");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FFmpeg availability");
            return false;
        }
    }

    public async Task<string> GetVersionAsync()
    {
        try
        {
            var result = await ExecuteVersionCommandAsync();

            if (!result.IsSuccess)
                return "Error";

            // Extract version from output
            var match = Regex.Match(result.Output, @"ffmpeg version (\S+)");
            return match.Success ? match.Groups[1].Value : "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FFmpeg version");
            return "Error";
        }
    }

    private async Task<FFmpegResult> ExecuteVersionCommandAsync()
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = "-version",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new FFmpegResult(
            process.ExitCode,
            output,
            error,
            process.ExitCode == 0,
            TimeSpan.Zero);
    }

    private static string GetSystemFFmpegPath()
    {
        return OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
    }
}
