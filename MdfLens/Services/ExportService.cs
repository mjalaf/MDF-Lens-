using System;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MdfLens.Services;

public static class ExportService
{
    public static async Task ExportToCsvAsync(DataTable table, string filePath)
    {
        var sb = new StringBuilder();

        // Header
        for (int i = 0; i < table.Columns.Count; i++)
        {
            sb.Append(EscapeCsv(table.Columns[i].ColumnName));
            if (i < table.Columns.Count - 1) sb.Append(',');
        }
        sb.AppendLine();

        // Rows
        foreach (DataRow row in table.Rows)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var val = row[i];
                sb.Append(val == DBNull.Value ? "" : EscapeCsv(val.ToString() ?? ""));
                if (i < table.Columns.Count - 1) sb.Append(',');
            }
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public static async Task ExportToTsvAsync(DataTable table, string filePath)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < table.Columns.Count; i++)
        {
            sb.Append(table.Columns[i].ColumnName.Replace("\t", " ").Replace("\r", "").Replace("\n", ""));
            if (i < table.Columns.Count - 1) sb.Append('\t');
        }
        sb.AppendLine();

        foreach (DataRow row in table.Rows)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var val = row[i];
                var str = val == DBNull.Value ? "" : (val.ToString() ?? "").Replace("\t", " ").Replace("\r", "").Replace("\n", " ");
                sb.Append(str);
                if (i < table.Columns.Count - 1) sb.Append('\t');
            }
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public static async Task ExportToJsonAsync(DataTable table, string filePath)
    {
        var rowsList = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object?>>();

        foreach (DataRow row in table.Rows)
        {
            var dict = new System.Collections.Generic.Dictionary<string, object?>();
            foreach (DataColumn col in table.Columns)
            {
                var val = row[col];
                dict[col.ColumnName] = val == DBNull.Value ? null : val;
            }
            rowsList.Add(dict);
        }

        var json = JsonSerializer.Serialize(rowsList, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }

    public static async Task ExportToSqlScriptAsync(DataTable table, string tableName, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"-- Export of table {tableName}");
        sb.AppendLine($"-- Generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        var colNames = new string[table.Columns.Count];
        for (int i = 0; i < table.Columns.Count; i++)
        {
            colNames[i] = $"[{table.Columns[i].ColumnName}]";
        }
        var columnsPart = string.Join(", ", colNames);

        foreach (DataRow row in table.Rows)
        {
            var valList = new System.Collections.Generic.List<string>();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var val = row[i];
                if (val == DBNull.Value)
                {
                    valList.Add("NULL");
                }
                else if (val is bool b)
                {
                    valList.Add(b ? "1" : "0");
                }
                else if (val is byte or short or int or long or float or double or decimal)
                {
                    valList.Add(Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture) ?? "0");
                }
                else if (val is DateTime dt)
                {
                    valList.Add($"'{dt:yyyy-MM-dd HH:mm:ss.fff}'");
                }
                else
                {
                    valList.Add($"N'{val.ToString()?.Replace("'", "''")}'");
                }
            }

            sb.AppendLine($"INSERT INTO {tableName} ({columnsPart}) VALUES ({string.Join(", ", valList)});");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
