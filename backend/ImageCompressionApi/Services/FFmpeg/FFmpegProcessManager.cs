using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using ImageCompressionApi.Models;
using ImageCompressionApi.Exceptions;
using Microsoft.Extensions.Options;

namespace ImageCompressionApi.Services.FFmpeg;

/// <summary>
/// Implementation of FFmpeg process manager
/// </summary>
public class FFmpegProcessManager : IFFmpegProcessManager
{
    private readonly ILogger<FFmpegProcessManager> _logger;
    private readonly ImageCompressionSettings _settings;

    public FFmpegProcessManager(
        ILogger<FFmpegProcessManager> logger,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value.ImageCompression;
    }

    public async Task ExecuteCompressionAsync(
        string inputPath,
        string outputPath,
        ImageFormatInfo format,
        int quality,
        CancellationToken cancellationToken = default)
    {
        var ffmpegPath = GetFFmpegPath();
        var arguments = BuildFFmpegArguments(inputPath, outputPath, format, quality);

        _logger.LogDebug("Running FFmpeg: {Path} {Arguments}", ffmpegPath, arguments);

        var processInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for process to complete with timeout
        var processTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_settings.FFmpegTimeoutSeconds), cancellationToken);

        var completedTask = await Task.WhenAny(processTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            process.Kill();
            throw new TimeoutException($"FFmpeg operation timed out after {_settings.FFmpegTimeoutSeconds} seconds");
        }

        if (process.ExitCode != 0)
        {
            var error = errorBuilder.ToString();
            var output = outputBuilder.ToString();
            _logger.LogError("FFmpeg failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            throw new FFmpegException(
                "FFmpeg compression failed",
                process.ExitCode,
                output,
                error);
        }

        _logger.LogDebug("FFmpeg completed successfully");
    }

    public async Task<bool> IsFFmpegAvailableAsync()
    {
        try
        {
            var ffmpegPath = GetFFmpegPath();
            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && output.Contains("ffmpeg version");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FFmpeg availability");
            return false;
        }
    }

    public async Task<string> GetFFmpegVersionAsync()
    {
        try
        {
            var ffmpegPath = GetFFmpegPath();
            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Extract version from output
            var match = Regex.Match(output, @"ffmpeg version (\S+)");
            return match.Success ? match.Groups[1].Value : "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FFmpeg version");
            return "Error";
        }
    }

    private string BuildFFmpegArguments(string inputPath, string outputPath, ImageFormatInfo format, int quality)
    {
        var args = new List<string>
        {
            "-i", $"\"{inputPath}\"",
            "-y", // Overwrite output file
            "-f", format.FFmpegFormat,
            "-vcodec", format.FFmpegCodec
        };

        // Add quality or compression parameters based on format
        if (format.SupportsQuality && !string.IsNullOrEmpty(format.QualityParameter))
        {
            args.AddRange(new[] { format.QualityParameter, quality.ToString() });
        }
        else if (format.DefaultCompressionLevel.HasValue)
        {
            args.AddRange(new[] { "-compression_level", format.DefaultCompressionLevel.Value.ToString() });
        }

        args.Add($"\"{outputPath}\"");

        return string.Join(" ", args);
    }

    private string GetFFmpegPath()
    {
        if (!string.IsNullOrEmpty(_settings.FFmpegPath))
        {
            return _settings.FFmpegPath;
        }

        // Try to find FFmpeg in system PATH
        var ffmpegName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        return ffmpegName;
    }
}
