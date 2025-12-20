// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintMigrator"/>.
/// </summary>
/// <param name="tableMigrator">The <see cref="ITableMigrator{TContext}"/> for migrating tables.</param>
/// <param name="logger">The <see cref="ILogger{UniqueConstraintMigrator}"/> for logging.</param>
[Singleton]
public class UniqueConstraintMigrator(
    ITableMigrator<UniqueConstraintDbContext> tableMigrator,
    ILogger<UniqueConstraintMigrator> logger) : IUniqueConstraintMigrator
{
    /// <inheritdoc/>
    public Task EnsureTableMigrated(string tableName, UniqueConstraintDbContext context) =>
        tableMigrator.EnsureTableMigrated(tableName, context, CreateTable);

    async Task CreateTable(UniqueConstraintDbContext context, string tableName)
    {
        logger.CreatingUniqueConstraintTable(tableName);

        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);

        migrationBuilder.CreateTable(
            name: tableName,
            columns: table => new
            {
                EventSourceId = table.StringColumn(migrationBuilder),
                Value = table.StringColumn(migrationBuilder),
                SequenceNumber = table.Column<decimal>(nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{tableName}", x => x.EventSourceId));

        migrationBuilder.CreateIndex(
            name: $"IX_{tableName}_Value",
            table: tableName,
            column: "Value");

        await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
    }
}


