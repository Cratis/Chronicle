// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintMigrator"/>.
/// </summary>
/// <param name="tableMigrator">The <see cref="ITableMigrator{TContext}"/> for migrating tables.</param>
/// <param name="valuesTableMigrator">The migrator for per-value tables.</param>
/// <param name="logger">The <see cref="ILogger{UniqueConstraintMigrator}"/> for logging.</param>
public class UniqueConstraintMigrator(
    ITableMigrator<UniqueConstraintDbContext> tableMigrator,
    ITableMigrator<UniqueConstraintValuesDbContext> valuesTableMigrator,
    ILogger<UniqueConstraintMigrator> logger) : IUniqueConstraintMigrator
{
    static readonly ConcurrentDictionary<string, bool> _indexMigrations = new();
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _migrationLocks = new();

    /// <inheritdoc/>
    public async Task EnsureTableMigrated(string tableName, UniqueConstraintDbContext context)
    {
        var cacheKey = $"{context.Database.GetConnectionString()}:{tableName}:value-unique";
        if (_indexMigrations.ContainsKey(cacheKey))
        {
            return;
        }

        var migrationLock = _migrationLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await migrationLock.WaitAsync();
        try
        {
            if (_indexMigrations.ContainsKey(cacheKey))
            {
                return;
            }

            await tableMigrator.EnsureTableMigrated(tableName, context, CreateTable);
            await UniqueConstraintValueIndexMigration.EnsureValueIndexIsUnique(tableName, context, logger);

            // A duplicate fallback stays cached until the next process start (or an explicit reset).
            _indexMigrations.TryAdd(cacheKey, true);
        }
        finally
        {
            migrationLock.Release();
        }
    }

    /// <inheritdoc/>
    public Task EnsureValuesTableMigrated(string tableName, UniqueConstraintValuesDbContext context) =>
        valuesTableMigrator.EnsureTableMigrated(tableName, context, CreateValuesTable);

    /// <inheritdoc/>
    public void ClearMigrationCache(string connectionStringPrefix)
    {
        tableMigrator.ClearMigrationCacheForConnectionString(connectionStringPrefix);
        valuesTableMigrator.ClearMigrationCacheForConnectionString(connectionStringPrefix);
        foreach (var key in _indexMigrations.Keys.Where(k => k.StartsWith(connectionStringPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            _indexMigrations.TryRemove(key, out _);
        }
    }

    async Task CreateValuesTable(UniqueConstraintValuesDbContext context, string tableName)
    {
        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);
        migrationBuilder.CreateTable(
            name: tableName,
            columns: table => new
            {
                Value = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                EventSourceId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                SequenceNumber = table.NumberColumn<decimal>(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_" + tableName, _ => _.Value));
        migrationBuilder.CreateIndex(name: "IX_" + tableName + "_EventSourceId", table: tableName, column: "EventSourceId");
        await valuesTableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
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
                Value = context.Database.IsSqlServer()
                    ? table.StringColumn(migrationBuilder, maxLength: 200, nullable: false)
                    : table.StringColumn(migrationBuilder, nullable: false),
                SequenceNumber = table.NumberColumn<decimal>(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_" + tableName, x => x.EventSourceId));
        migrationBuilder.CreateIndex(name: "IX_" + tableName + "_Value", table: tableName, column: "Value", unique: true);
        await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
    }
}
