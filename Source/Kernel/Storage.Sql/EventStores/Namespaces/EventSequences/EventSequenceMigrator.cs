// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceMigrator"/>.
/// </summary>
/// <param name="tableMigrator">The <see cref="ITableMigrator{TContext}"/> for migrating tables.</param>
/// <param name="logger">The <see cref="ILogger{EventSequenceMigrator}"/> for logging.</param>
[Singleton]
public class EventSequenceMigrator(
    ITableMigrator<EventSequenceDbContext> tableMigrator,
    ILogger<EventSequenceMigrator> logger) : IEventSequenceMigrator
{
    /// <inheritdoc/>
    public Task EnsureTableMigrated(string tableName, EventSequenceDbContext context) =>
        tableMigrator.EnsureTableMigrated(tableName, context, CreateTable);

    async Task CreateTable(EventSequenceDbContext context, string tableName)
    {
        logger.CreatingEventSequenceTable(tableName);

        var migrationBuilder = new MigrationBuilder(context.Database.ProviderName);

        migrationBuilder.CreateTable(
            name: tableName,
            columns: table => new
            {
                SequenceNumber = table.Column<ulong>(nullable: false),
                CorrelationId = table.StringColumn(migrationBuilder),
                Causation = table.StringColumn(migrationBuilder),
                CausedBy = table.StringColumn(migrationBuilder),
                Type = table.StringColumn(migrationBuilder),
                Occurred = table.Column<DateTimeOffset>(nullable: false),
                EventSourceType = table.StringColumn(migrationBuilder),
                EventSourceId = table.StringColumn(migrationBuilder),
                EventStreamType = table.StringColumn(migrationBuilder),
                EventStreamId = table.StringColumn(migrationBuilder),
                Content = table.StringColumn(migrationBuilder),
                Compensations = table.JsonColumn<IDictionary<string, string>>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{tableName}", x => x.SequenceNumber));

        migrationBuilder.CreateIndex(
            name: $"IX_{tableName}_SequenceNumber",
            table: tableName,
            column: "SequenceNumber");

        migrationBuilder.CreateIndex(
            name: $"IX_{tableName}_EventSourceId",
            table: tableName,
            column: "EventSourceId");

        migrationBuilder.CreateIndex(
            name: $"IX_{tableName}_Type",
            table: tableName,
            column: "Type");

        migrationBuilder.CreateIndex(
            name: $"IX_{tableName}_Occurred",
            table: tableName,
            column: "Occurred");

        migrationBuilder.CreateIndex(
            name: $"IX_{tableName}_EventStreamType_EventStreamId",
            table: tableName,
            columns: ["EventStreamType", "EventStreamId"]);

        await tableMigrator.ExecuteMigrationOperations(context, migrationBuilder);
    }
}
