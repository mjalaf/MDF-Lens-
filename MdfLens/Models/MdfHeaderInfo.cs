namespace MdfLens.Models;

public class MdfHeaderInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public long TotalPages { get; set; }
    public int InternalVersion { get; set; }
    public string SqlVersionName { get; set; } = string.Empty;
    public string Collation { get; set; } = string.Empty;
    public DateTime? CreationDate { get; set; }
    public bool IsSupportedByLocalDb { get; set; }
    public bool IsAttached { get; set; }
    public string AttachedDatabaseName { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
}
