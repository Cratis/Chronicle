// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;
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
[Singleton]
public class TableMigrator<TContext>(ILogger<TableMigrator<TContext>> logger) : ITableMigrator<TContext>
    where TContext : DbContext
{
    static readonly ConcurrentDictionary<string, bool> _migratedTables = new();
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _migrationLocks = new();

    /// <inheritdoc/>
    public async Task EnsureTableMigrated(string tableName, TContext context, Func<TContext, string, Task> createTableAction)
    {
        var key = $"{context.Database.GetConnectionString()}:{tableName}";

        if (_migratedTables.ContainsKey(key))
        {
            return;
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
                var hasTable = await TableExists(context, tableName);

                if (!hasTable)
                {
                    await createTableAction(context, tableName);
                    logger.CreatedTable(tableName);
                }
                else
                {
                    logger.TableAlreadyExists(tableName);
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
    public async Task ExecuteMigrationOperations(TContext context, MigrationBuilder migrationBuilder)
    {
        var operations = migrationBuilder.Operations;
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

    static async Task<bool> TableExists(TContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();

            if (context.Database.IsSqlServer())
            {
                command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
            }
            else if (context.Database.IsNpgsql())
            {
                command.CommandText = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}'";
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
}
