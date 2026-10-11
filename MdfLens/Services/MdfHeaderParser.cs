using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MdfLens.Models;

namespace MdfLens.Services;

public static class MdfHeaderParser
{
    private const int PageSize = 8192;

    public static MdfHeaderInfo ParseFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("MDF file not found.", filePath);
        }

        var header = new MdfHeaderInfo
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            FileSizeBytes = fileInfo.Length,
            TotalPages = fileInfo.Length / PageSize,
            FileSizeFormatted = FormatBytes(fileInfo.Length)
        };

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            
            // 1. Read Page 0 (File Header Page)
            byte[] page0 = new byte[PageSize];
            int read0 = fs.Read(page0, 0, PageSize);

            // 2. Read Page 9 (Boot Page / DBINFO) if file is large enough
            byte[] page9 = new byte[PageSize];
            if (fs.Length >= 10 * PageSize)
            {
                fs.Seek(9 * PageSize, SeekOrigin.Begin);
                fs.Read(page9, 0, PageSize);

                // Offset 100 in Page 9 holds the DB version
                header.InternalVersion = BitConverter.ToUInt16(page9, 100);
            }

            header.SqlVersionName = ResolveSqlVersion(header.InternalVersion);
            header.IsSupportedByLocalDb = header.InternalVersion >= 655;

            // Extract Database Name from Page 0 or Page 9 strings
            header.DatabaseName = ExtractDatabaseName(page0, page9, Path.GetFileNameWithoutExtension(filePath));
            header.StatusMessage = $"MDF parsed successfully. Version: {header.SqlVersionName} ({header.InternalVersion})";
        }
        catch (Exception ex)
        {
            header.StatusMessage = $"Warning parsing binary header: {ex.Message}";
            header.SqlVersionName = "SQL Server MDF";
            header.DatabaseName = Path.GetFileNameWithoutExtension(filePath);
        }

        return header;
    }

    private static string ResolveSqlVersion(int version)
    {
        return version switch
        {
            515 => "SQL Server 7.0",
            539 => "SQL Server 2000",
            611 or 612 => "SQL Server 2005",
            655 => "SQL Server 2008",
            661 => "SQL Server 2008 R2",
            706 => "SQL Server 2012",
            782 => "SQL Server 2014",
            852 => "SQL Server 2016",
            869 => "SQL Server 2017",
            895 => "SQL Server 2019",
            957 => "SQL Server 2022",
            > 957 => $"SQL Server (v{version})",
            > 0 => $"SQL Server {version}",
            _ => "SQL Server Database"
        };
    }

    private static string ExtractDatabaseName(byte[] page0, byte[] page9, string defaultName)
    {
        try
        {
            // First check page 9 for clean unicode identifier strings
            var text9 = Encoding.Unicode.GetString(page9);
            var matches = Regex.Matches(text9, @"[a-zA-Z0-9_]{3,64}");
            foreach (Match m in matches)
            {
                var val = m.Value;
                // Exclude common internal keywords
                if (val != "master" && val != "model" && val != "tempdb" && val != "msdb" &&
                    !val.StartsWith("PRIMARY", StringComparison.OrdinalIgnoreCase) &&
                    !val.StartsWith("sys", StringComparison.OrdinalIgnoreCase))
                {
                    return val;
                }
            }

            // Check page 0
            var text0 = Encoding.Unicode.GetString(page0);
            matches = Regex.Matches(text0, @"[a-zA-Z0-9_]{3,64}");
            foreach (Match m in matches)
            {
                var val = m.Value;
                if (val != "master" && val != "model" && val != "tempdb" && val != "msdb" &&
                    !val.StartsWith("PRIMARY", StringComparison.OrdinalIgnoreCase) &&
                    !val.StartsWith("sys", StringComparison.OrdinalIgnoreCase))
                {
                    return val;
                }
            }
        }
        catch
        {
            // Fallback
        }

        return defaultName;
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}
