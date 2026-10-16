// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

#pragma warning disable EF1002 // Identifiers are delimited by the provider; metadata values are parameters.

/// <summary>
/// Upgrades the per-source value index without discarding existing claims.
/// </summary>
internal static class UniqueConstraintValueIndexMigration
{
    static readonly ConcurrentDictionary<string, bool> _warnedTables = new();

    /// <summary>
    /// Settles new claims through a unique index, or preserves duplicate legacy claims with a warning.
    /// </summary>
    /// <param name="tableName">The constraint table.</param>
    /// <param name="context">The context owning the connection.</param>
    /// <param name="logger">The migration logger.</param>
    /// <returns>The migration task.</returns>
    internal static async Task EnsureValueIndexIsUnique(string tableName, UniqueConstraintDbContext context, ILogger<UniqueConstraintMigrator> logger)
    {
        // Hold off writers across the guards and DDL, including writers in other silos.
        await using var transaction = await context.Database.BeginTransactionAsync();
        var sql = context.GetService<ISqlGenerationHelper>();
        var table = sql.DelimitIdentifier(tableName);
        var index = sql.DelimitIdentifier($"IX_{tableName}_Value");
        await LockTable(context, table);

        var indexState = await ReadIndexState(context, tableName);
        if (indexState == 1)
        {
            await transaction.CommitAsync();
            return;
        }

        var canIndex = !context.Database.IsSqlServer() || await EnsureSqlServerValueLength(context, tableName, table, index, logger);
        indexState = await ReadIndexState(context, tableName);
        var duplicates = await Scalar(context, $"SELECT COUNT(*) FROM (SELECT {sql.DelimitIdentifier("Value")} FROM {table} GROUP BY {sql.DelimitIdentifier("Value")} HAVING COUNT(*) > 1) AS duplicate_values");
        if (canIndex && (duplicates == 0 || indexState < 0))
        {
            if (indexState == 0)
            {
                await DropIndex(context, table, index);
            }

            await context.Database.ExecuteSqlRawAsync($"CREATE {(duplicates == 0 ? "UNIQUE " : string.Empty)}INDEX {index} ON {table} ({sql.DelimitIdentifier("Value")})");
        }

        await transaction.CommitAsync();

        if (duplicates > 0 && _warnedTables.TryAdd($"{context.Database.GetConnectionString()}:{tableName}", true))
        {
            logger.FallingBackToNonUniqueIndex(tableName, duplicates);
        }
    }

    static async Task LockTable(UniqueConstraintDbContext context, string table)
    {
        if (context.Database.IsNpgsql())
        {
            await context.Database.ExecuteSqlRawAsync($"LOCK TABLE {table} IN EXCLUSIVE MODE");
        }
        else if (context.Database.IsSqlServer())
        {
            await Scalar(context, $"SELECT COUNT_BIG(*) FROM {table} WITH (TABLOCKX, HOLDLOCK)");
        }

        // SQLite's transaction owns the write lock before the read checks below.
    }

    static async Task<bool> EnsureSqlServerValueLength(UniqueConstraintDbContext context, string tableName, string table, string index, ILogger<UniqueConstraintMigrator> logger)
    {
        var length = await Scalar(context, "SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = @table AND COLUMN_NAME = 'Value'", tableName);
        if (length == 200)
        {
            return true;
        }

        // DATALENGTH includes trailing spaces and counts UTF-16 code units, unlike LEN.
        var oversized = await Scalar(context, $"SELECT COUNT_BIG(*) FROM {table} WHERE DATALENGTH([Value]) > 400");
        var legacy = await Scalar(context, $"SELECT COUNT_BIG(*) FROM {table} WHERE DATALENGTH([Value]) <> 128 OR [Value] COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9a-f]%'");
        if (oversized > 0 || legacy > 0)
        {
            logger.SkippingValueColumnNarrowing(tableName, oversized, legacy);
            return false;
        }

        // The pre-check and ALTER share the same transaction and exclusive table lock.
        await DropIndex(context, table, index);
        await context.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ALTER COLUMN [Value] NVARCHAR(200) NOT NULL");
        return true;
    }

    static Task<long> ReadIndexState(UniqueConstraintDbContext context, string tableName)
    {
        var query = "SELECT \"unique\" FROM pragma_index_list(@table) WHERE name = @index";
        if (context.Database.IsSqlServer())
        {
            query = "SELECT CAST(is_unique AS INT) FROM sys.indexes WHERE object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME(@table)) AND name = @index";
        }
        else if (context.Database.IsNpgsql())
        {
            query = "SELECT CASE WHEN i.indisunique THEN 1 ELSE 0 END FROM pg_index i JOIN pg_class idx ON idx.oid = i.indexrelid JOIN pg_class tbl ON tbl.oid = i.indrelid JOIN pg_namespace ns ON ns.oid = tbl.relnamespace WHERE ns.nspname = current_schema() AND tbl.relname = CAST(@table AS name) AND idx.relname = CAST(@index AS name)";
        }

        return Scalar(context, query, tableName);
    }

    static Task<int> DropIndex(UniqueConstraintDbContext context, string table, string index) => context.Database.IsSqlServer()
        ? context.Database.ExecuteSqlRawAsync($"DROP INDEX IF EXISTS {index} ON {table}")
        : context.Database.ExecuteSqlRawAsync($"DROP INDEX IF EXISTS {index}");

    static async Task<long> Scalar(UniqueConstraintDbContext context, string query, string? tableName = null)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // Identifiers are delimited by the provider; values are parameters.
        command.CommandText = query;
#pragma warning restore CA2100
        command.Transaction = context.Database.CurrentTransaction!.GetDbTransaction();
        if (tableName is not null)
        {
            var table = command.CreateParameter();
            table.ParameterName = "@table";
            table.Value = tableName;
            command.Parameters.Add(table);
            var index = command.CreateParameter();
            index.ParameterName = "@index";
            index.Value = $"IX_{tableName}_Value";
            command.Parameters.Add(index);
        }

        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? -1 : Convert.ToInt64(result);
    }
}
