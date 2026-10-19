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
    /// <summary>
    /// The maximum length of the subject column, kept within every provider's index key limit.
    /// </summary>
    internal const int SubjectMaxLength = 200;

    const string SubjectColumn = nameof(UniqueConstraintIndexEntry.Subject);

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

            await tableMigrator.EnsureTableMigrated(tableName, context, CreateTable, (ctx, name) => AddSubjectColumn(tableMigrator, ctx, name));
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
        valuesTableMigrator.EnsureTableMigrated(tableName, context, CreateValuesTable, (ctx, name) => AddSubjectColumn(valuesTableMigrator, ctx, name));

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

    /// <summary>
    /// Adds the nullable subject column and its index to a table created before claims recorded their subject.
    /// </summary>
    /// <remarks>
    /// Every row the table already holds keeps a null subject, which erasure leaves alone, so adding it changes no existing claim.
    /// Another kernel can add the column first, so a failure that leaves the column in place is success.
    /// </remarks>
    /// <typeparam name="TContext">The type of context.</typeparam>
    /// <param name="migrator">The table migrator.</param>
    /// <param name="context">The context connected to the namespace database.</param>
    /// <param name="tableName">The table to upgrade.</param>
    /// <returns>Awaitable task.</returns>
    static async Task AddSubjectColumn<TContext>(ITableMigrator<TContext> migrator, TContext context, string tableName)
        where TContext : DbContext
    {
        if (await migrator.ColumnExists(context, tableName, SubjectColumn)) return;

        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);
        migrationBuilder.AddColumn<string>(name: SubjectColumn, table: tableName, maxLength: SubjectMaxLength, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_" + tableName + "_" + SubjectColumn, table: tableName, column: SubjectColumn);
        try
        {
            await migrator.ExecuteMigrationOperations(context, migrationBuilder);
        }
        catch (Exception)
        {
            // Lost the race to another kernel when the column now exists.
            if (!await migrator.ColumnExists(context, tableName, SubjectColumn)) throw;
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
                SequenceNumber = table.NumberColumn<decimal>(migrationBuilder, nullable: false),
                Subject = table.StringColumn(migrationBuilder, maxLength: SubjectMaxLength, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_" + tableName, _ => _.Value));
        migrationBuilder.CreateIndex(name: "IX_" + tableName + "_EventSourceId", table: tableName, column: "EventSourceId");
        migrationBuilder.CreateIndex(name: "IX_" + tableName + "_" + SubjectColumn, table: tableName, column: SubjectColumn);
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
                SequenceNumber = table.NumberColumn<decimal>(migrationBuilder, nullable: false),
                Subject = table.StringColumn(migrationBuilder, maxLength: SubjectMaxLength, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_" + tableName, x => x.EventSourceId));
        migrationBuilder.CreateIndex(name: "IX_" + tableName + "_Value", table: tableName, column: "Value", unique: true);
        migrationBuilder.CreateIndex(name: "IX_" + tableName + "_" + SubjectColumn, table: tableName, column: SubjectColumn);
        await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
    }
}
