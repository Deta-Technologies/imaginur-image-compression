using ImageCompressionApi.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace ImageCompressionApi.Services;

/// <summary>
/// Service for image compression using FFmpeg
/// </summary>
public class ImageCompressionService : IImageCompressionService
{
    private readonly ILogger<ImageCompressionService> _logger;
    private readonly ImageCompressionSettings _settings;
    private readonly string _tempDirectory;
    private readonly SemaphoreSlim _semaphore;

    public ImageCompressionService(
        ILogger<ImageCompressionService> logger,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value.ImageCompression;
        _tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");
        _semaphore = new SemaphoreSlim(_settings.MaxConcurrentOperations, _settings.MaxConcurrentOperations);

        // Ensure temp directory exists
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Compress an image file using FFmpeg
    /// </summary>
    public async Task<ImageCompressionResult> CompressImageAsync(
        IFormFile file, 
        int quality, 
        string? format = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Wait for available slot
            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                _logger.LogInformation("Starting compression for file: {FileName}", file.FileName);

                // Generate unique file ID
                var fileId = Guid.NewGuid().ToString("N");
                var originalExtension = Path.GetExtension(file.FileName);
                var inputFileName = $"{fileId}_original{originalExtension}";
                var inputFilePath = Path.Combine(_tempDirectory, inputFileName);

                // Determine output format and extension
                var outputFormat = DetermineOutputFormat(format, originalExtension);
                var outputExtension = GetExtensionForFormat(outputFormat);
                var outputFileName = $"{fileId}_compressed{outputExtension}";
                var outputFilePath = Path.Combine(_tempDirectory, outputFileName);

                // Save uploaded file
                await using (var fileStream = new FileStream(inputFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream, cancellationToken);
                }

                var originalSize = new FileInfo(inputFilePath).Length;
                _logger.LogInformation("Original file size: {Size} bytes", originalSize);

                // Compress using FFmpeg
                await RunFFmpegCompressionAsync(inputFilePath, outputFilePath, outputFormat, quality, cancellationToken);

                // Get compressed file size
                var compressedSize = new FileInfo(outputFilePath).Length;
                var compressionRatio = (1.0 - (double)compressedSize / originalSize) * 100;

                _logger.LogInformation("Compression completed. Original: {Original} bytes, Compressed: {Compressed} bytes, Ratio: {Ratio:F1}%", 
                    originalSize, compressedSize, compressionRatio);

                // Clean up original file
                File.Delete(inputFilePath);

                stopwatch.Stop();

                return new ImageCompressionResult
                {
                    OriginalSize = originalSize,
                    CompressedSize = compressedSize,
                    CompressionRatio = compressionRatio,
                    Format = outputFormat,
                    DownloadUrl = $"/image/download/{fileId}",
                    FileId = fileId,
                    CompressedAt = DateTime.UtcNow,
                    Quality = quality,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image: {FileName}", file.FileName);
            throw;
        }
    }

    /// <summary>
    /// Get the compressed file by ID
    /// </summary>
    public async Task<(Stream FileStream, string ContentType, string FileName)?> GetCompressedFileAsync(string fileId)
    {
        try
        {
            // Find the compressed file
            var pattern = $"{fileId}_compressed.*";
            var files = Directory.GetFiles(_tempDirectory, pattern);
            
            if (files.Length == 0)
            {
                _logger.LogWarning("Compressed file not found for ID: {FileId}", fileId);
                return null;
            }

            var filePath = files[0];
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = GetContentTypeForExtension(extension);

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            return (fileStream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compressed file: {FileId}", fileId);
            return null;
        }
    }

    /// <summary>
    /// Check if FFmpeg is available
    /// </summary>
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

    /// <summary>
    /// Get FFmpeg version information
    /// </summary>
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

    /// <summary>
    /// Clean up expired temporary files
    /// </summary>
    public async Task<int> CleanupExpiredFilesAsync()
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-_settings.TempFileRetentionMinutes);
            var files = Directory.GetFiles(_tempDirectory);
            var deletedCount = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoffTime)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogDebug("Deleted expired file: {FileName}", fileInfo.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete expired file: {FileName}", fileInfo.Name);
                    }
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired temporary files", deletedCount);
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of expired files");
            return 0;
        }
    }

    /// <summary>
    /// Run FFmpeg compression
    /// </summary>
    private async Task RunFFmpegCompressionAsync(
        string inputPath, 
        string outputPath, 
        string format, 
        int quality, 
        CancellationToken cancellationToken)
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
            _logger.LogError("FFmpeg failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            throw new InvalidOperationException($"FFmpeg compression failed: {error}");
        }

        _logger.LogDebug("FFmpeg completed successfully");
    }

    /// <summary>
    /// Build FFmpeg arguments for compression
    /// </summary>
    private string BuildFFmpegArguments(string inputPath, string outputPath, string format, int quality)
    {
        var args = new List<string>
        {
            "-i", $"\"{inputPath}\"",
            "-y" // Overwrite output file
        };

        switch (format.ToLowerInvariant())
        {
            case "jpeg":
            case "jpg":
                args.AddRange(new[] { "-f", "image2", "-vcodec", "mjpeg", "-q:v", quality.ToString() });
                break;
            case "png":
                args.AddRange(new[] { "-f", "image2", "-vcodec", "png", "-compression_level", "9" });
                break;
            case "webp":
                args.AddRange(new[] { "-f", "webp", "-c:v", "libwebp", "-q:v", quality.ToString() });
                break;
            case "bmp":
                args.AddRange(new[] { "-f", "image2", "-vcodec", "bmp" });
                break;
            default:
                throw new ArgumentException($"Unsupported format: {format}");
        }

        args.Add($"\"{outputPath}\"");

        return string.Join(" ", args);
    }

    /// <summary>
    /// Get FFmpeg executable path
    /// </summary>
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

    /// <summary>
    /// Determine output format based on input
    /// </summary>
    private string DetermineOutputFormat(string? requestedFormat, string originalExtension)
    {
        if (!string.IsNullOrEmpty(requestedFormat) && requestedFormat != "same")
        {
            return requestedFormat.ToLowerInvariant();
        }

        return originalExtension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "jpeg",
            ".png" => "png",
            ".webp" => "webp",
            ".bmp" => "bmp",
            _ => "jpeg" // Default fallback
        };
    }

    /// <summary>
    /// Get file extension for format
    /// </summary>
    private string GetExtensionForFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => ".jpg",
            "png" => ".png",
            "webp" => ".webp",
            "bmp" => ".bmp",
            _ => ".jpg"
        };
    }

    /// <summary>
    /// Get content type for file extension
    /// </summary>
    private string GetContentTypeForExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }
} 