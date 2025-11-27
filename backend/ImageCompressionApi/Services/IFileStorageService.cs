namespace ImageCompressionApi.Services;

/// <summary>
/// Abstraction for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Save a file to storage
    /// </summary>
    Task<string> SaveFileAsync(Stream stream, string fileId, string extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a file from storage
    /// </summary>
    Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(string pattern);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task DeleteFileAsync(string filePath);

    /// <summary>
    /// Get file size
    /// </summary>
    Task<long> GetFileSizeAsync(string filePath);

    /// <summary>
    /// Find files matching a pattern
    /// </summary>
    Task<string[]> FindFilesAsync(string directory, string pattern);

    /// <summary>
    /// Get all files in a directory with creation time
    /// </summary>
    Task<IEnumerable<FileMetadata>> GetFilesWithMetadataAsync(string directory);

    /// <summary>
    /// Check if file exists
    /// </summary>
    Task<bool> FileExistsAsync(string filePath);
}

/// <summary>
/// File metadata including path, creation time, and size
/// </summary>
public record FileMetadata(string FilePath, DateTime CreationTimeUtc, long Size);
