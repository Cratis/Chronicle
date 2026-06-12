// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

#pragma warning disable CA2100 // Table and column names are internal constants, not user input.

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintMigrator"/>.
/// </summary>
/// <param name="tableMigrator">The <see cref="ITableMigrator{TContext}"/> for migrating tables.</param>
/// <param name="logger">The <see cref="ILogger{UniqueConstraintMigrator}"/> for logging.</param>
public class UniqueConstraintMigrator(
    ITableMigrator<UniqueConstraintDbContext> tableMigrator,
    ILogger<UniqueConstraintMigrator> logger) : IUniqueConstraintMigrator
{
    static readonly ConcurrentDictionary<string, bool> _columnMigrations = new();

    /// <inheritdoc/>
    public async Task EnsureTableMigrated(string tableName, UniqueConstraintDbContext context)
    {
        await tableMigrator.EnsureTableMigrated(tableName, context, CreateTable);
        await EnsureValueColumnIsUnbounded(tableName, context);
    }

    /// <inheritdoc/>
    public void ClearMigrationCache(string connectionStringPrefix)
    {
        tableMigrator.ClearMigrationCacheForConnectionString(connectionStringPrefix);
        foreach (var key in _columnMigrations.Keys.Where(k => k.StartsWith(connectionStringPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            _columnMigrations.TryRemove(key, out _);
        }
    }

    static async Task<bool> ValueColumnNeedsWidening(UniqueConstraintDbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();

            if (context.Database.IsNpgsql())
            {
                command.CommandText =
                    "SELECT character_maximum_length FROM information_schema.columns " +
                    "WHERE table_schema = 'public' AND table_name = '" + tableName + "' AND column_name = 'Value'";
            }
            else if (context.Database.IsSqlServer())
            {
                command.CommandText =
                    "SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS " +
                    "WHERE TABLE_NAME = '" + tableName + "' AND COLUMN_NAME = 'Value'";
            }
            else
            {
                return false;
            }

            var result = await command.ExecuteScalarAsync();
            if (result is null || result is DBNull)
            {
                return false;
            }

            var maxLength = Convert.ToInt32(result);
            return maxLength > 0 && maxLength <= 200;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    async Task CreateTable(UniqueConstraintDbContext context, string tableName)
    {
        logger.CreatingUniqueConstraintTable(tableName);

        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);

        migrationBuilder.CreateTable(
            name: tableName,
            columns: table => new
            {
                EventSourceId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Value = table.StringColumn(migrationBuilder),
                SequenceNumber = table.NumberColumn<decimal>(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_" + tableName, x => x.EventSourceId));

        // SQL Server does not allow indexes on NVARCHAR(MAX) / VARCHAR(MAX) columns.
        // Omit the index for SQL Server; the WHERE Value = ? query remains correct without it.
        if (!context.Database.IsSqlServer())
        {
            migrationBuilder.CreateIndex(
                name: "IX_" + tableName + "_Value",
                table: tableName,
                column: "Value");
        }

        await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
    }

    async Task EnsureValueColumnIsUnbounded(string tableName, UniqueConstraintDbContext context)
    {
        var connectionString = context.Database.GetConnectionString() ?? string.Empty;
        var cacheKey = connectionString + ":" + tableName + ":value-unbounded";

        if (_columnMigrations.ContainsKey(cacheKey))
        {
            return;
        }

        if (!await ValueColumnNeedsWidening(context, tableName))
        {
            _columnMigrations.TryAdd(cacheKey, true);
            return;
        }

        logger.WideningValueColumn(tableName);

        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);

        if (context.Database.IsNpgsql())
        {
            migrationBuilder.Sql("ALTER TABLE \"" + tableName + "\" ALTER COLUMN \"Value\" TYPE TEXT");
        }
        else if (context.Database.IsSqlServer())
        {
            // SQL Server cannot ALTER a column to NVARCHAR(MAX) while an index references it,
            // and cannot create indexes on MAX-type columns at all. Drop the index first, widen
            // the column, and leave it un-indexed (the constraint check query is still correct).
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_" + tableName + "_Value' AND object_id = OBJECT_ID('" + tableName + "')) " +
                "DROP INDEX [IX_" + tableName + "_Value] ON [" + tableName + "]");
            migrationBuilder.Sql("ALTER TABLE [" + tableName + "] ALTER COLUMN [Value] NVARCHAR(MAX)");
        }

        if (migrationBuilder.Operations.Count > 0)
        {
            await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
        }

        _columnMigrations.TryAdd(cacheKey, true);
    }
}
