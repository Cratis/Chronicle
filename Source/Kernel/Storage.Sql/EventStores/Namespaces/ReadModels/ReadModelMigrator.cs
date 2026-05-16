// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelMigrator"/>.
/// </summary>
/// <param name="tableMigrator">The <see cref="ITableMigrator{TContext}"/> for migrating tables.</param>
/// <param name="logger">The <see cref="ILogger{ReadModelMigrator}"/> for logging.</param>
[Singleton]
public class ReadModelMigrator(
    ITableMigrator<ReadModelDbContext> tableMigrator,
    ILogger<ReadModelMigrator> logger) : IReadModelMigrator
{
    /// <inheritdoc/>
    public Task EnsureTableMigrated(string tableName, ReadModelDbContext context) =>
        tableMigrator.EnsureTableMigrated(tableName, context, CreateTable);

    async Task CreateTable(ReadModelDbContext context, string tableName)
    {
        logger.CreatingReadModelTable(tableName);

        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);

        migrationBuilder.CreateTable(
            name: tableName,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Document = table.StringColumn(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{tableName}", x => x.Id));

        await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
    }
}
