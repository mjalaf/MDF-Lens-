namespace MdfLens.Models;

public class ColumnInfo
{
    public int ColumnId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public short MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
}
