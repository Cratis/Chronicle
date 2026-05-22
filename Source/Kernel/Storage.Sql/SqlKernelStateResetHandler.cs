// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Wipes the SQL backing store between integration test specs without restarting the container.
/// SQLite is reset by deleting the database files; PostgreSQL and Microsoft SQL Server are reset
/// by truncating every user table in the database.
/// For SQLite, connection pools are cleared before file deletion to release OS file handles.
/// For PostgreSQL and MsSql, pool clearing is intentionally skipped — table truncation leaves
/// connections valid, and forcibly closing all kernel connections causes the OAuth endpoint to
/// return 500 during the reconnection window.
/// </summary>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> describing the active storage backend.</param>
/// <param name="database">The <see cref="IDatabase"/> whose per-context migration cache must be cleared on reset so the next request re-runs EF Core migrations.</param>
public class SqlKernelStateResetHandler(IOptions<ChronicleOptions> options, IDatabase database) : ICanPerformKernelStateReset
{
    /// <summary>
    /// Tables that survive a reset. These carry identity and Data Protection key material;
    /// wiping them would invalidate the test client's JWT (the kernel rotates signing keys
    /// when the table is empty) and force every test-class boundary to fail with 401 even
    /// though the bootstrap handler can recreate the rows.
    /// </summary>
    static readonly HashSet<string> _preservedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        WellKnownTableNames.Users,
        WellKnownTableNames.Applications,
        WellKnownTableNames.DataProtectionKeys,
        WellKnownTableNames.Patches,
        WellKnownTableNames.SystemInformation,
        WellKnownTableNames.EncryptionKeys,
        WellKnownTableNames.Tokens,
        WellKnownTableNames.Authorizations,
        WellKnownTableNames.Scopes,
        "__EFMigrationsHistory",
    };

    /// <inheritdoc/>
    public bool CanReset()
    {
        var type = options.Value.Storage.Type;
        return string.Equals(type, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async Task Reset()
    {
        var storageType = options.Value.Storage.Type;
        var connectionDetails = options.Value.Storage.ConnectionDetails;

        // Drop the per-context migration cache so EF Core re-runs migrations on the next
        // request. Required for SQLite (we delete the file outright), and harmless on
        // PostgreSQL / MS SQL where MigrateAsync becomes a no-op when nothing is pending.
        database.ClearTableMigrationCache(string.Empty);

        if (string.Equals(storageType, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            // SQLite: close all pooled handles, then delete every database file the kernel
            // could have created. The kernel creates one file per event-store namespace
            // (chronicle.db, chronicle_Testing.db, chronicle_Testing_default.db, etc.), so
            // we delete the base file plus all underscore-suffixed variants in the same dir.
            SqliteConnection.ClearAllPools();

            var path = ExtractSqliteDataSource(connectionDetails);
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                directory = ".";
            }

            var baseName = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            foreach (var file in Directory.GetFiles(directory, $"{baseName}_*{extension}"))
            {
                File.Delete(file);
            }
        }
        else if (string.Equals(storageType, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            await TruncateAllPostgreSqlTables(connectionDetails);
        }
        else if (string.Equals(storageType, StorageType.MsSql, StringComparison.OrdinalIgnoreCase))
        {
            await TruncateAllMsSqlTables(connectionDetails);
        }
    }

    static string ExtractSqliteDataSource(string connectionString)
    {
        var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connectionString };
        if (builder.TryGetValue("Data Source", out var value) || builder.TryGetValue("Filename", out value))
        {
            return value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    static string PreservedNamesForSql() =>
        string.Join(", ", _preservedTables.Select(t => $"'{t}'"));

    static async Task TruncateAllPostgreSqlTables(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        try
        {
            await conn.OpenAsync();
        }
        catch (NpgsqlException)
        {
            // Database may not exist yet on the first reset — nothing to truncate.
            return;
        }

        var preserved = PreservedNamesForSql();
        var sql =
            "DO $$ DECLARE r RECORD; " +
            "BEGIN " +
            "    FOR r IN (SELECT tablename FROM pg_tables " +
            $"              WHERE schemaname = 'public' AND tablename NOT IN ({preserved})) LOOP " +
            "        EXECUTE 'TRUNCATE TABLE \"' || r.tablename || '\" CASCADE'; " +
            "    END LOOP; " +
            "END $$;";
#pragma warning disable CA2100 // Preserved-table names are compile-time constants from WellKnownTableNames.
        await using var cmd = new NpgsqlCommand(sql, conn);
#pragma warning restore CA2100
        await cmd.ExecuteNonQueryAsync();
    }

    static async Task TruncateAllMsSqlTables(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        try
        {
            await conn.OpenAsync();
        }
        catch (SqlException)
        {
            // Database may not exist yet on the first reset — nothing to truncate.
            return;
        }

        var preserved = PreservedNamesForSql();
        var sql =
            "DECLARE @sql NVARCHAR(MAX) = N''; " +
            "SELECT @sql += N'ALTER TABLE [' + s.name + N'].[' + t.name + N'] NOCHECK CONSTRAINT ALL;' + CHAR(13) " +
            "    FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id " +
            $"    WHERE t.name NOT IN ({preserved}); " +
            "SELECT @sql += N'DELETE FROM [' + s.name + N'].[' + t.name + N'];' + CHAR(13) " +
            "    FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id " +
            $"    WHERE t.name NOT IN ({preserved}); " +
            "SELECT @sql += N'ALTER TABLE [' + s.name + N'].[' + t.name + N'] WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13) " +
            "    FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id " +
            $"    WHERE t.name NOT IN ({preserved}); " +
            "EXEC sp_executesql @sql;";
#pragma warning disable CA2100 // Preserved-table names are compile-time constants from WellKnownTableNames.
        await using var cmd = new SqlCommand(sql, conn);
#pragma warning restore CA2100
        await cmd.ExecuteNonQueryAsync();
    }
}
