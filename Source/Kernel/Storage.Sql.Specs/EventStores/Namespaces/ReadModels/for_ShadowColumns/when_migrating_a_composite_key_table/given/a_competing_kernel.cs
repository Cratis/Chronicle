// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumns.when_migrating_a_composite_key_table.given;

internal sealed class a_competing_kernel(ITableMigrator<ReadModelDbContext> inner) : ITableMigrator<ReadModelDbContext>
{
    int _added;
    internal int Duplicates { get; private set; }

    public Task EnsureTableMigrated(string tableName, ReadModelDbContext context, Func<ReadModelDbContext, string, Task> createTableAction, Func<ReadModelDbContext, string, Task>? upgradeTableAction = null) =>
        inner.EnsureTableMigrated(tableName, context, createTableAction, upgradeTableAction);

    public Task<bool> ColumnExists(ReadModelDbContext context, string tableName, string columnName) => inner.ColumnExists(context, tableName, columnName);

    public void ClearMigrationCacheForConnectionString(string connectionStringPrefix) => inner.ClearMigrationCacheForConnectionString(connectionStringPrefix);

    public async Task ExecuteMigrationOperations(ReadModelDbContext context, MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.Operations.Exists(IsShadowAddition) && Interlocked.CompareExchange(ref _added, 1, 0) == 0)
        {
            // An older kernel does not take our advisory lock. It wins the DDL race on another connection.
            var builder = new DbContextOptionsBuilder<ReadModelDbContext>();
            builder.UseDatabaseFromConnectionString(context.Database.GetConnectionString()!);
            await using var competing = new ReadModelDbContext(builder.Options, context.TableName, context.Columns, Substitute.For<IReadModelMigrator>());
            await inner.ExecuteMigrationOperations(competing, migrationBuilder);
        }
        try
        {
            await inner.ExecuteMigrationOperations(context, migrationBuilder);
        }
        catch
        {
            Duplicates++;
            throw;
        }
    }

    static bool IsShadowAddition(MigrationOperation operation) => operation is AddColumnOperation column && string.Equals(column.Name, "__compositeKey", StringComparison.Ordinal);
}
