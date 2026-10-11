using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MdfLens.Models;

namespace MdfLens.Services;

public class SqlServerService : IDisposable
{
    private const string MasterConnectionString = "Server=(LocalDB)\\MSSQLLocalDB;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;";
    
    private string? _activeDatabaseName;
    private bool _isDynamicallyAttached;

    public string? ActiveDatabaseName => _activeDatabaseName;

    public static async Task<bool> IsLocalDbAvailableAsync()
    {
        try
        {
            await using var conn = new SqlConnection(MasterConnectionString);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<(string dbName, string filePath)>> GetRegisteredDatabasesAsync()
    {
        var list = new List<(string dbName, string filePath)>();
        try
        {
            await using var conn = new SqlConnection(MasterConnectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT d.name, mf.physical_name 
                FROM sys.sysdatabases d
                INNER JOIN sys.master_files mf ON d.dbid = mf.database_id
                WHERE mf.type = 0 
                  AND d.name NOT IN ('master', 'model', 'msdb', 'tempdb')
                ORDER BY d.name";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var db = reader.GetString(0);
                var file = reader.GetString(1);
                if (File.Exists(file))
                {
                    list.Add((db, file));
                }
            }
        }
        catch { }
        return list;
    }

    public async Task<(bool success, string dbName, string message)> AttachOrConnectAsync(string mdfPath)
    {
        var normalizedMdfPath = Path.GetFullPath(mdfPath).ToLowerInvariant();

        await using var conn = new SqlConnection(MasterConnectionString);
        await conn.OpenAsync();

        // 1. Check if database is already attached in LocalDB
        var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = @"
            SELECT d.name 
            FROM sys.sysdatabases d
            INNER JOIN sys.master_files mf ON d.dbid = mf.database_id
            WHERE LOWER(mf.physical_name) = @path";
        checkCmd.Parameters.AddWithValue("@path", normalizedMdfPath);

        var existingName = await checkCmd.ExecuteScalarAsync() as string;
        if (!string.IsNullOrEmpty(existingName))
        {
            _activeDatabaseName = existingName;
            _isDynamicallyAttached = false;
            return (true, existingName, $"Connected to already attached database '{existingName}'.");
        }

        // 2. Database is not attached yet, attach dynamically
        var tempDbName = "MdfView_" + Guid.NewGuid().ToString("N")[..8];
        try
        {
            var attachCmd = conn.CreateCommand();
            attachCmd.CommandText = $"CREATE DATABASE [{tempDbName}] ON (FILENAME = '{mdfPath.Replace("'", "''")}') FOR ATTACH";
            await attachCmd.ExecuteNonQueryAsync();

            _activeDatabaseName = tempDbName;
            _isDynamicallyAttached = true;
            return (true, tempDbName, $"Attached successfully as '{tempDbName}'.");
        }
        catch (Exception ex1)
        {
            if (ex1.Message.Contains("cannot be upgraded", StringComparison.OrdinalIgnoreCase) ||
                ex1.Message.Contains("not supported by this version", StringComparison.OrdinalIgnoreCase))
            {
                return (false, string.Empty, ex1.Message);
            }

            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                var rebuildCmd = conn.CreateCommand();
                rebuildCmd.CommandText = $"CREATE DATABASE [{tempDbName}] ON (FILENAME = '{mdfPath.Replace("'", "''")}') FOR ATTACH_REBUILD_LOG";
                await rebuildCmd.ExecuteNonQueryAsync();

                _activeDatabaseName = tempDbName;
                _isDynamicallyAttached = true;
                return (true, tempDbName, $"Attached successfully with rebuilt log as '{tempDbName}'.");
            }
            catch
            {
                return (false, string.Empty, ex1.Message);
            }
        }
    }

    public async Task<List<TableItem>> GetTablesAsync(string dbName)
    {
        var tables = new List<TableItem>();
        var connStr = $"Server=(LocalDB)\\MSSQLLocalDB;Database={dbName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;";

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                s.name AS SchemaName, 
                t.name AS TableName,
                ISNULL(SUM(p.rows), 0) AS [RowCount],
                CAST(0 AS bit) AS IsView
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            LEFT JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
            WHERE t.is_ms_shipped = 0
            GROUP BY s.name, t.name

            UNION ALL

            SELECT 
                s.name AS SchemaName,
                v.name AS TableName,
                0 AS [RowCount],
                CAST(1 AS bit) AS IsView
            FROM sys.views v
            INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
            WHERE v.is_ms_shipped = 0
            ORDER BY IsView ASC, TableName ASC;";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(new TableItem
            {
                Schema = reader.GetString(0),
                Name = reader.GetString(1),
                RowCount = reader.GetInt64(2),
                IsView = reader.GetBoolean(3)
            });
        }

        return tables;
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(string dbName, string schema, string table)
    {
        var columns = new List<ColumnInfo>();
        var connStr = $"Server=(LocalDB)\\MSSQLLocalDB;Database={dbName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;";

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                c.column_id AS ColumnId,
                c.name AS ColumnName,
                TYPE_NAME(c.user_type_id) + 
                    CASE 
                        WHEN TYPE_NAME(c.user_type_id) IN ('varchar', 'char', 'varbinary') THEN '(' + CASE WHEN c.max_length = -1 THEN 'max' ELSE CAST(c.max_length AS varchar) END + ')'
                        WHEN TYPE_NAME(c.user_type_id) IN ('nvarchar', 'nchar') THEN '(' + CASE WHEN c.max_length = -1 THEN 'max' ELSE CAST(c.max_length / 2 AS varchar) END + ')'
                        WHEN TYPE_NAME(c.user_type_id) IN ('decimal', 'numeric') THEN '(' + CAST(c.precision AS varchar) + ',' + CAST(c.scale AS varchar) + ')'
                        ELSE ''
                    END AS DataType,
                c.max_length AS MaxLength,
                c.is_nullable AS IsNullable,
                c.is_identity AS IsIdentity,
                ISNULL(OBJECT_DEFINITION(c.default_object_id), '') AS DefaultValue,
                ISNULL(i.is_primary_key, 0) AS IsPrimaryKey
            FROM sys.columns c
            LEFT JOIN (
                SELECT ic.object_id, ic.column_id, idx.is_primary_key
                FROM sys.index_columns ic
                INNER JOIN sys.indexes idx ON ic.object_id = idx.object_id AND ic.index_id = idx.index_id
                WHERE idx.is_primary_key = 1
            ) i ON c.object_id = i.object_id AND c.column_id = i.column_id
            WHERE c.object_id = OBJECT_ID(@fullTable)
            ORDER BY c.column_id;";

        cmd.Parameters.AddWithValue("@fullTable", $"[{schema}].[{table}]");

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnInfo
            {
                ColumnId = reader.GetInt32(0),
                ColumnName = reader.GetString(1),
                DataType = reader.GetString(2),
                MaxLength = reader.GetInt16(3),
                IsNullable = reader.GetBoolean(4),
                IsIdentity = reader.GetBoolean(5),
                DefaultValue = reader.GetString(6),
                IsPrimaryKey = reader.GetBoolean(7)
            });
        }

        return columns;
    }

    public async Task<(DataTable dataTable, long totalRows)> GetTableDataAsync(
        string dbName,
        string schema,
        string table,
        int pageNumber,
        int pageSize,
        string? filterColumn,
        string? filterValue,
        List<ColumnInfo> columns)
    {
        var connStr = $"Server=(LocalDB)\\MSSQLLocalDB;Database={dbName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        string whereClause = string.Empty;
        var cmd = conn.CreateCommand();

        if (!string.IsNullOrWhiteSpace(filterValue))
        {
            if (string.IsNullOrEmpty(filterColumn) || filterColumn == "(All Columns)")
            {
                var orList = new List<string>();
                for (int i = 0; i < columns.Count; i++)
                {
                    var col = columns[i];
                    var paramName = $"@p{i}";
                    orList.Add($"CAST([{col.ColumnName}] AS nvarchar(max)) LIKE {paramName}");
                    cmd.Parameters.AddWithValue(paramName, $"%{filterValue}%");
                }
                if (orList.Count > 0)
                {
                    whereClause = "WHERE " + string.Join(" OR ", orList);
                }
            }
            else
            {
                whereClause = $"WHERE CAST([{filterColumn}] AS nvarchar(max)) LIKE @p0";
                cmd.Parameters.AddWithValue("@p0", $"%{filterValue}%");
            }
        }

        // Count total rows matching filter
        cmd.CommandText = $"SELECT COUNT_BIG(*) FROM [{schema}].[{table}] {whereClause};";
        long totalRows = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Order by first column by default for reliable pagination
        string orderBy = columns.Count > 0 ? $"[{columns[0].ColumnName}]" : "(SELECT NULL)";

        int offset = Math.Max(0, (pageNumber - 1) * pageSize);
        string dataSql = $@"
            SELECT * FROM [{schema}].[{table}]
            {whereClause}
            ORDER BY {orderBy}
            OFFSET {offset} ROWS
            FETCH NEXT {pageSize} ROWS ONLY;";

        cmd.CommandText = dataSql;

        var dt = new DataTable();
        using var adapter = new SqlDataAdapter(cmd);
        adapter.Fill(dt);

        return (dt, totalRows);
    }

    public async Task<DataTable> ExecuteQueryAsync(string dbName, string sql)
    {
        var connStr = $"Server=(LocalDB)\\MSSQLLocalDB;Database={dbName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var dt = new DataTable();
        using var adapter = new SqlDataAdapter(cmd);
        adapter.Fill(dt);

        return dt;
    }

    public async Task DetachCurrentDatabaseAsync()
    {
        if (_isDynamicallyAttached && !string.IsNullOrEmpty(_activeDatabaseName))
        {
            try
            {
                await using var conn = new SqlConnection(MasterConnectionString);
                await conn.OpenAsync();

                var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    ALTER DATABASE [{_activeDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    EXEC sp_detach_db '{_activeDatabaseName}';";
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                _isDynamicallyAttached = false;
                _activeDatabaseName = null;
            }
        }
        else
        {
            _activeDatabaseName = null;
        }
    }

    public void Dispose()
    {
        if (_isDynamicallyAttached && !string.IsNullOrEmpty(_activeDatabaseName))
        {
            try
            {
                using var conn = new SqlConnection(MasterConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    ALTER DATABASE [{_activeDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    EXEC sp_detach_db '{_activeDatabaseName}';";
                cmd.ExecuteNonQuery();
            }
            catch { }
        }
    }
}
