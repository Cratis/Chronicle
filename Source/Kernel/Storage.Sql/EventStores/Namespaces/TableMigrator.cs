// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

/// <summary>
/// Represents an implementation of <see cref="ITableMigrator{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">The type of <see cref="DbContext"/>.</typeparam>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
public class TableMigrator<TContext>(ILogger<TableMigrator<TContext>> logger) : ITableMigrator<TContext>
    where TContext : DbContext
{
    static readonly ConcurrentDictionary<string, bool> _migratedTables = new();
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _migrationLocks = new();

    /// <inheritdoc/>
    public async Task EnsureTableMigrated(string tableName, TContext context, Func<TContext, string, Task> createTableAction, Func<TContext, string, Task>? upgradeTableAction = null)
    {
        if (DynamicSchemaVerification.IsActive)
        {
            await createTableAction(context, tableName);
            return;
        }
        var key = $"{context.Database.GetConnectionString()}:{tableName}";

        if (_migratedTables.ContainsKey(key))
        {
            // The database may have been dropped and recreated (e.g. between integration tests),
            // which removes all tables while leaving the static cache intact. Verify the table
            // still exists; if not, evict the stale cache entry so it is recreated below.
            if (await TableExists(context, tableName))
            {
                return;
            }

            _migratedTables.TryRemove(key, out _);
        }

        // Get or create a lock for this specific table
        var lockObject = _migrationLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await lockObject.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_migratedTables.ContainsKey(key))
            {
                return;
            }

            logger.EnsuringTableMigrated(tableName);

            try
            {
                // An existing table without an upgrade needs no schema change, so it does not wait for other kernels.
                if (upgradeTableAction is null && await TableExists(context, tableName))
                {
                    logger.TableAlreadyExists(tableName);
                }
                else
                {
                    await DatabaseSchemaLock.Run(context, () => CreateOrUpgradeTable(tableName, context, createTableAction, upgradeTableAction));
                }

                _migratedTables.TryAdd(key, true);
            }
            catch (Exception ex)
            {
                logger.FailedToMigrateTable(ex, tableName);
                throw;
            }
        }
        finally
        {
            lockObject.Release();
        }
    }

    /// <inheritdoc/>
    public void ClearMigrationCacheForConnectionString(string connectionStringPrefix)
    {
        foreach (var key in _migratedTables.Keys.Where(k => k.StartsWith(connectionStringPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            _migratedTables.TryRemove(key, out _);
        }
    }

    /// <inheritdoc/>
    public async Task ExecuteMigrationOperations(TContext context, MigrationBuilder migrationBuilder)
    {
        if (DynamicSchemaVerification.IsActive)
        {
            await DynamicSchemaVerification.Verify(context, migrationBuilder);
            return;
        }
        var operations = migrationBuilder.Operations;
        var guard = DynamicSchemaGuard.For(context);
        using var warnings = guard.FlushOnDispose();
        foreach (var operation in operations)
        {
            switch (operation)
            {
                case Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation table:
                    guard.Require(context, "table", table.Name);
                    break;
                case Microsoft.EntityFrameworkCore.Migrations.Operations.ColumnOperation column:
                    guard.Require(context, "column", $"{column.Table}.{column.Name}");
                    break;
                case Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation index:
                    guard.Require(context, "index", $"{index.Table}.{index.Name}");
                    break;
            }
        }
        var sqlGenerator = context.Database.GetService<IMigrationsSqlGenerator>();
        var commands = sqlGenerator.Generate(operations, context.Model);

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var command in commands)
                {
                    await using var dbCommand = connection.CreateCommand();
                    dbCommand.CommandText = command.CommandText;
                    dbCommand.Transaction = transaction;
                    await dbCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ColumnExists(TContext context, string tableName, string columnName)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();

            if (context.Database.IsSqlServer())
            {
                command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";
            }
            else if (context.Database.IsNpgsql())
            {
                command.CommandText = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = '{tableName}' AND column_name = '{columnName}'";
            }
            else
            {
                command.CommandText = $"SELECT COUNT(*) FROM pragma_table_xinfo('{tableName}') WHERE name = '{columnName}'";
            }

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    static async Task<bool> TryCreateTable(string tableName, TContext context, Func<TContext, string, Task> createTableAction)
    {
        var guard = DynamicSchemaGuard.For(context);
        using var warnings = guard.FlushOnDispose();
        guard.Require(context, "table", tableName);
        try
        {
            await createTableAction(context, tableName);
            return true;
        }
        catch (Exception ex) when (SchemaObjectAlreadyExists.Is(ex))
        {
            // A kernel not taking the schema lock may have created it first. Its table is adopted like any existing one.
            if (!await TableExists(context, tableName))
            {
                throw;
            }

            return false;
        }
    }

    static async Task<bool> TableExists(TContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();

            if (context.Database.IsSqlServer())
            {
                command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = '{tableName}'";
            }
            else if (context.Database.IsNpgsql())
            {
                command.CommandText = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = '{tableName}'";
            }
            else
            {
                command.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            }

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    async Task CreateOrUpgradeTable(string tableName, TContext context, Func<TContext, string, Task> createTableAction, Func<TContext, string, Task>? upgradeTableAction)
    {
        // Another kernel may have created the table while this one waited for the lock, so look again.
        if (!await TableExists(context, tableName) && await TryCreateTable(tableName, context, createTableAction))
        {
            logger.CreatedTable(tableName);
            return;
        }

        logger.TableAlreadyExists(tableName);
        if (upgradeTableAction is null)
        {
            return;
        }

        try
        {
            await upgradeTableAction(context, tableName);
        }
        catch (Exception ex) when (SchemaObjectAlreadyExists.Is(ex))
        {
            // A kernel not taking the schema lock changed it at the same time. The upgrade checks what is
            // there before changing anything, so running it again settles what is left.
            await upgradeTableAction(context, tableName);
        }
    }
}
