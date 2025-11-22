namespace ImageCompressionApi.Models;

/// <summary>
/// Cleanup operation result
/// </summary>
public class CleanupResult
{
    public int DeletedCount { get; set; }
    public DateTime CleanupTime { get; set; }
}
