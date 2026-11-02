// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Records the DDL that reaches <see cref="ITableMigrator{TContext}"/>. The migrator runs its statements as raw ADO
/// commands that EF interceptors never see, so this is where schema changes are observed without a production seam.
/// </summary>
/// <typeparam name="TContext">The context type.</typeparam>
/// <param name="inner">The migrator that does the work.</param>
/// <param name="capture">Gets the capture to record into, when there is one.</param>
public class RecordingTableMigrator<TContext>(ITableMigrator<TContext> inner, Func<SqlCommandCapture?> capture) : ITableMigrator<TContext>
    where TContext : DbContext
{
    /// <inheritdoc/>
    public Task EnsureTableMigrated(string tableName, TContext context, Func<TContext, string, Task> createTableAction, Func<TContext, string, Task>? upgradeTableAction = null) =>
        inner.EnsureTableMigrated(tableName, context, createTableAction, upgradeTableAction);

    /// <inheritdoc/>
    public Task<bool> ColumnExists(TContext context, string tableName, string columnName) => inner.ColumnExists(context, tableName, columnName);

    /// <inheritdoc/>
    public Task ExecuteMigrationOperations(TContext context, MigrationBuilder migrationBuilder)
    {
        foreach (var operation in migrationBuilder.Operations)
        {
            capture()?.RecordMigration(operation switch
            {
                CreateTableOperation create => $"CREATE TABLE {create.Name}",
                CreateIndexOperation index => $"CREATE INDEX {index.Name} ON {index.Table}",
                _ => $"{operation.GetType().Name} {operation}"
            });
        }

        return inner.ExecuteMigrationOperations(context, migrationBuilder);
    }

    /// <inheritdoc/>
    public void ClearMigrationCacheForConnectionString(string connectionStringPrefix) => inner.ClearMigrationCacheForConnectionString(connectionStringPrefix);
}
