using ImageCompressionApi.Models;

namespace ImageCompressionApi.Services.Storage;

/// <summary>
/// Implementation of temporary file manager
/// </summary>
public class TemporaryFileManager : ITemporaryFileManager
{
    private readonly ILogger<TemporaryFileManager> _logger;
    private readonly string _tempDirectory;

    public TemporaryFileManager(ILogger<TemporaryFileManager> logger)
    {
        _logger = logger;
        _tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");

        // Ensure temp directory exists
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task<string> SaveUploadedFileAsync(IFormFile file, string fileId, CancellationToken cancellationToken = default)
    {
        var originalExtension = Path.GetExtension(file.FileName);
        var fileName = $"{fileId}_original{originalExtension}";
        var filePath = Path.Combine(_tempDirectory, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream, cancellationToken);

        _logger.LogDebug("Saved uploaded file: {FileName} to {Path}", file.FileName, filePath);
        return filePath;
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)?> GetCompressedFileAsync(string fileId)
    {
        FileStream? fileStream = null;
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
            var formatInfo = ImageFormatProvider.GetByExtension(extension);
            var contentType = formatInfo?.MimeTypes[0] ?? "application/octet-stream";

            // Open the file stream - caller must dispose
            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var result = (fileStream, contentType, fileName);
            fileStream = null; // Transfer ownership to caller
            return result;
        }
        catch (Exception ex)
        {
            // Ensure stream is disposed if an error occurs
            fileStream?.Dispose();
            _logger.LogError(ex, "Error retrieving compressed file: {FileId}", fileId);
            return null;
        }
    }

    public Task DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted file: {Path}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file: {Path}", filePath);
        }

        return Task.CompletedTask;
    }

    public async Task<int> CleanupExpiredFilesAsync(TimeSpan retentionTime)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - retentionTime;
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

    public FileInfo GetFileInfo(string filePath)
    {
        return new FileInfo(filePath);
    }
}
