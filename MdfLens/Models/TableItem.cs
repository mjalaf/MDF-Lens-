namespace MdfLens.Models;

public class TableItem
{
    public string Schema { get; set; } = "dbo";
    public string Name { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public bool IsView { get; set; }

    public string FullName => $"[{Schema}].[{Name}]";

    public string RowCountFormatted
    {
        get
        {
            if (RowCount >= 1_000_000)
                return $"{RowCount / 1_000_000.0:F1}M rows";
            if (RowCount >= 1_000)
                return $"{RowCount / 1_000.0:F1}K rows";
            return $"{RowCount} rows";
        }
    }
}
