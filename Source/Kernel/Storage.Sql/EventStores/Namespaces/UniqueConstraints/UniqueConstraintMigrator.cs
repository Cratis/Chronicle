// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
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

    const int MaxIdentifierLength = 63;
    const string SubjectColumn = nameof(UniqueConstraintIndexEntry.Subject);

    static readonly ConcurrentDictionary<string, bool> _indexMigrations = new();
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _migrationLocks = new();

    /// <inheritdoc/>
    public Task EnsureTableMigrated(string tableName, UniqueConstraintDbContext context) => EnsureMigrated(context, tableName, async () =>
    {
        await tableMigrator.EnsureTableMigrated(tableName, context, CreateTable, (ctx, name) => AddSubjectColumn(tableMigrator, ctx, name));
        await using var transaction = context.Database.IsSqlServer() && context.Database.CurrentTransaction is null ? await context.Database.BeginTransactionAsync() : null;
        await UniqueConstraintOrdinalColumns.EnsureMigrated(context, tableName);
        await UniqueConstraintValueIndexMigration.EnsureValueIndexIsUnique(tableName, context, logger);
        if (transaction is not null) await transaction.CommitAsync();
    });

    /// <inheritdoc/>
    public Task EnsureValuesTableMigrated(string tableName, UniqueConstraintValuesDbContext context) => EnsureMigrated(context, tableName, async () =>
    {
        await valuesTableMigrator.EnsureTableMigrated(tableName, context, CreateValuesTable, (ctx, name) => AddSubjectColumn(valuesTableMigrator, ctx, name));
        await UniqueConstraintOrdinalColumns.EnsureMigrated(context, tableName);
    });

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
    /// Gets the name of the index on the subject column, kept within PostgreSQL's identifier limit.
    /// </summary>
    /// <param name="tableName">The claim table.</param>
    /// <returns>The index name.</returns>
    internal static string SubjectIndexName(string tableName)
    {
        var name = "IX_" + tableName + "_" + SubjectColumn;
        if (Encoding.UTF8.GetByteCount(name) <= MaxIdentifierLength) return name;

        // PostgreSQL truncates identifiers longer than its limit in bytes, which would collide with the table's other
        // indexes, so keep a hash of the full name where the truncation cannot remove it.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..8];
        var suffix = "_" + hash + "_" + SubjectColumn;
        var prefixLength = 0;
        var prefixBytes = 0;
        while (prefixLength < name.Length)
        {
            var next = char.IsSurrogatePair(name, prefixLength) ? 2 : 1;
            var bytes = Encoding.UTF8.GetByteCount(name.AsSpan(prefixLength, next));
            if (prefixBytes + bytes > MaxIdentifierLength - suffix.Length) break;
            prefixBytes += bytes;
            prefixLength += next;
        }

        return name[..prefixLength] + suffix;
    }

    static async Task EnsureMigrated(DbContext context, string tableName, Func<Task> migrate)
    {
        var cacheKey = $"{context.Database.GetConnectionString()}:{tableName}:value-unique:ordinal-v1";
        if (_indexMigrations.ContainsKey(cacheKey)) return;

        var migrationLock = _migrationLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await migrationLock.WaitAsync();
        try
        {
            if (_indexMigrations.ContainsKey(cacheKey)) return;
            await DatabaseSchemaLock.Run(context, migrate);

            // A duplicate fallback stays cached until the next process start (or an explicit reset).
            _indexMigrations.TryAdd(cacheKey, true);
        }
        finally
        {
            migrationLock.Release();
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
        migrationBuilder.CreateIndex(name: SubjectIndexName(tableName), table: tableName, column: SubjectColumn);
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

    static Task CreateClaimTable<TContext>(ITableMigrator<TContext> migrator, TContext context, string tableName, bool perValue)
        where TContext : DbContext
    {
        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);
        migrationBuilder.CreateTable(
            name: tableName,
            columns: columns => new
            {
                EventSourceId = columns.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Value = perValue || context.Database.IsSqlServer()
                    ? columns.StringColumn(migrationBuilder, maxLength: 200, nullable: false)
                    : columns.StringColumn(migrationBuilder, nullable: false),
                SequenceNumber = columns.NumberColumn<decimal>(migrationBuilder, nullable: false),
                Subject = columns.StringColumn(migrationBuilder, maxLength: SubjectMaxLength, nullable: true)
            },
            constraints: table =>
            {
                if (perValue) table.PrimaryKey("PK_" + tableName, row => row.Value);
                else table.PrimaryKey("PK_" + tableName, row => row.EventSourceId);
            });
        var indexed = perValue ? "EventSourceId" : "Value";
        migrationBuilder.CreateIndex(name: "IX_" + tableName + "_" + indexed, table: tableName, column: indexed, unique: !perValue);
        migrationBuilder.CreateIndex(name: SubjectIndexName(tableName), table: tableName, column: SubjectColumn);
        if (context.Database.IsSqlServer())
        {
            foreach (var column in migrationBuilder.Operations.OfType<CreateTableOperation>().Single().Columns.Where(column => column.Name == "EventSourceId" || column.Name == "Value"))
            {
                column.Collation = UniqueConstraintOrdinalColumns.SqlServerCollation;
            }
        }
        return migrator.ExecuteMigrationOperations(context, migrationBuilder);
    }

    Task CreateValuesTable(UniqueConstraintValuesDbContext context, string tableName) =>
        CreateClaimTable(valuesTableMigrator, context, tableName, true);

    Task CreateTable(UniqueConstraintDbContext context, string tableName)
    {
        logger.CreatingUniqueConstraintTable(tableName);
        return CreateClaimTable(tableMigrator, context, tableName, false);
    }
}
