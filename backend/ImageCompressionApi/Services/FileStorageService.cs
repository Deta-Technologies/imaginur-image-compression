using ImageCompressionApi.Constants;
using ImageCompressionApi.Models;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;

namespace ImageCompressionApi.Services;

/// <summary>
/// File storage service using abstracted file system
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _tempDirectory;

    public FileStorageService(
        IFileSystem fileSystem,
        ILogger<FileStorageService> logger,
        IOptions<AppSettings> settings)
    {
        _fileSystem = fileSystem;
        _logger = logger;

        var currentDir = _fileSystem.Directory.GetCurrentDirectory();
        _tempDirectory = _fileSystem.Path.Combine(currentDir, "wwwroot", "temp");

        // Ensure temp directory exists
        if (!_fileSystem.Directory.Exists(_tempDirectory))
        {
            _fileSystem.Directory.CreateDirectory(_tempDirectory);
        }
    }

    public async Task<string> SaveFileAsync(
        Stream stream,
        string fileId,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"{fileId}{extension}";
        var filePath = _fileSystem.Path.Combine(_tempDirectory, fileName);

        _logger.LogDebug("Saving file: {FileName} to {Path}", fileName, filePath);

        await using var fileStream = _fileSystem.File.Create(filePath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("File saved: {FileName}, Size: {Size} bytes",
            fileName, fileStream.Length);

        return filePath;
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(string pattern)
    {
        try
        {
            var files = _fileSystem.Directory.GetFiles(_tempDirectory, pattern);

            if (files.Length == 0)
            {
                _logger.LogWarning("No files found matching pattern: {Pattern}", pattern);
                return null;
            }

            var filePath = files[0];

            // Security: Verify path is within temp directory
            var fullPath = _fileSystem.Path.GetFullPath(filePath);
            var tempDirFullPath = _fileSystem.Path.GetFullPath(_tempDirectory);

            if (!fullPath.StartsWith(tempDirFullPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Path traversal detected: {Path}", fullPath);
                return null;
            }

            var fileName = _fileSystem.Path.GetFileName(filePath);
            var extension = _fileSystem.Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = GetContentTypeForExtension(extension);

            var fileStream = _fileSystem.File.OpenRead(filePath);

            return (fileStream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file with pattern: {Pattern}", pattern);
            return null;
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            if (_fileSystem.File.Exists(filePath))
            {
                await Task.Run(() => _fileSystem.File.Delete(filePath));
                _logger.LogDebug("Deleted file: {Path}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file: {Path}", filePath);
        }
    }

    public async Task<long> GetFileSizeAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var fileInfo = _fileSystem.FileInfo.New(filePath);
            return fileInfo.Length;
        });
    }

    public async Task<string[]> FindFilesAsync(string directory, string pattern)
    {
        return await Task.Run(() => _fileSystem.Directory.GetFiles(directory, pattern));
    }

    public async Task<IEnumerable<FileMetadata>> GetFilesWithMetadataAsync(string directory)
    {
        return await Task.Run(() =>
        {
            var files = _fileSystem.Directory.GetFiles(directory);
            return files.Select(filePath =>
            {
                var fileInfo = _fileSystem.FileInfo.New(filePath);
                return new FileMetadata(filePath, fileInfo.CreationTimeUtc, fileInfo.Length);
            }).ToList();
        });
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        return await Task.Run(() => _fileSystem.File.Exists(filePath));
    }

    private string GetContentTypeForExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => MimeTypes.Jpeg,
            ".png" => MimeTypes.Png,
            ".webp" => MimeTypes.WebP,
            ".bmp" => MimeTypes.Bmp,
            _ => "application/octet-stream"
        };
    }
}
