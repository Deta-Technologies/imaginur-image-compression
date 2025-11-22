namespace ImageCompressionApi.Services.Storage;

/// <summary>
/// Manages temporary file operations for image compression
/// </summary>
public interface ITemporaryFileManager
{
    /// <summary>
    /// Save an uploaded file to temporary storage
    /// </summary>
    Task<string> SaveUploadedFileAsync(IFormFile file, string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a compressed file by its ID
    /// </summary>
    Task<(Stream FileStream, string ContentType, string FileName)?> GetCompressedFileAsync(string fileId);

    /// <summary>
    /// Delete a specific file
    /// </summary>
    Task DeleteFileAsync(string filePath);

    /// <summary>
    /// Clean up files older than the specified retention time
    /// </summary>
    Task<int> CleanupExpiredFilesAsync(TimeSpan retentionTime);

    /// <summary>
    /// Get file information
    /// </summary>
    FileInfo GetFileInfo(string filePath);
}
