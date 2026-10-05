// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceMigrator"/>.
/// </summary>
/// <param name="tableMigrator">The <see cref="ITableMigrator{TContext}"/> for migrating tables.</param>
/// <param name="logger">The <see cref="ILogger{EventSequenceMigrator}"/> for logging.</param>
public class EventSequenceMigrator(
    ITableMigrator<EventSequenceDbContext> tableMigrator,
    ILogger<EventSequenceMigrator> logger) : IEventSequenceMigrator
{
    /// <summary>The reserved companion table shared by all sequences in a namespace database.</summary>
    public const string NamedTagsTable = "__cratis_named_tags";

    /// <inheritdoc/>
    public async Task EnsureTableMigrated(string tableName, EventSequenceDbContext context)
    {
        if (string.Equals(tableName, NamedTagsTable, StringComparison.OrdinalIgnoreCase))
        {
            throw new NamedTagsTableCollision();
        }

        await tableMigrator.EnsureTableMigrated(
            tableName,
            context,
            async (db, name) =>
            {
                await EnsureNamedTagsTable(db);
                await CreateTable(db, name);
            },
            async (db, name) =>
            {
                await EnsureNamedTagsTable(db);
                await UpgradeTable(db, name);
            });
    }

    /// <inheritdoc/>
    public void ClearMigrationCache(string connectionStringPrefix) =>
        tableMigrator.ClearMigrationCacheForConnectionString(connectionStringPrefix);

    static bool IsTableAlreadyExists(Exception exception) => exception switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.DuplicateTable,
        SqlException sql => sql.Number == 2714,
        SqliteException sqlite => sqlite.SqliteErrorCode == 1 && sqlite.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    Task EnsureNamedTagsTable(EventSequenceDbContext context) =>
        tableMigrator.EnsureTableMigrated(NamedTagsTable, context, CreateNamedTagsTable, ValidateNamedTagsTable);

    async Task CreateNamedTagsTable(EventSequenceDbContext context, string tableName)
    {
        var migration = new MigrationBuilder(context.Database.ProviderName);
        migration.CreateTable(
            name: tableName,
            columns: table => new
            {
                EventSequenceId = table.StringColumn(migration, maxLength: 200, nullable: false),
                SequenceNumber = table.Column<ulong>(nullable: false),
                Position = table.Column<int>(nullable: false),
                Name = table.Column<byte[]>(nullable: false),
                Value = table.Column<byte[]>(nullable: false),
                NameHash = table.Column<byte[]>(maxLength: 32, nullable: false),
                ValueHash = table.Column<byte[]>(maxLength: 32, nullable: false),
                CratisNamedTagsVersion = table.Column<int>(nullable: false, defaultValue: 1)
            },
            constraints: table => table.PrimaryKey("PK_cratis_named_tags", x => new { x.EventSequenceId, x.SequenceNumber, x.Position }));
        migration.CreateIndex("IX_cratis_tags_name", tableName, ["EventSequenceId", "NameHash", "SequenceNumber"]);
        migration.CreateIndex("IX_cratis_tags_value", tableName, ["EventSequenceId", "NameHash", "ValueHash", "SequenceNumber"]);
        migration.CreateIndex("IX_cratis_tags_event", tableName, ["EventSequenceId", "SequenceNumber"]);
        try
        {
            await tableMigrator.ExecuteMigrationOperations(context, migration);
        }
        catch (Exception exception) when (IsTableAlreadyExists(exception))
        {
            // Another silo won the first-touch race. Only adopt the table if it is ours.
            await ValidateNamedTagsTable(context, tableName);
        }
    }

    async Task ValidateNamedTagsTable(EventSequenceDbContext context, string tableName)
    {
        // A table with this name predating Chronicle's migration must not be adopted or modified.
        foreach (var column in new[] { "CratisNamedTagsVersion", "EventSequenceId", "SequenceNumber", "Position", "Name", "Value", "NameHash", "ValueHash" })
        {
            if (!await tableMigrator.ColumnExists(context, tableName, column))
            {
                throw new NamedTagsTableCollision();
            }
        }
    }

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
                Type = table.StringColumn(migrationBuilder, maxLength: 200),
                Occurred = table.Column<DateTimeOffset>(nullable: false),
                EventSourceType = table.StringColumn(migrationBuilder, maxLength: 200),
                EventSourceId = table.StringColumn(migrationBuilder, maxLength: 200),
                EventStreamType = table.StringColumn(migrationBuilder, maxLength: 200),
                EventStreamId = table.StringColumn(migrationBuilder, maxLength: 200),
                Content = table.StringColumn(migrationBuilder),
                ContentHashes = table.StringColumn(migrationBuilder),
                Compensations = table.JsonColumn<IDictionary<string, string>>(migrationBuilder),
                Subject = table.StringColumn(migrationBuilder, nullable: true),
                EventSource = table.StringColumn(migrationBuilder, nullable: true),
                Tags = table.StringColumn(migrationBuilder)
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

    async Task UpgradeTable(EventSequenceDbContext context, string tableName)
    {
        if (!await tableMigrator.ColumnExists(context, tableName, nameof(EventEntry.Tags)))
        {
            logger.AddingTagsColumn(tableName);

            var tagsMigration = new MigrationBuilder(context.Database.ProviderName);
            tagsMigration.AddColumn<string>(
                name: nameof(EventEntry.Tags),
                table: tableName,
                nullable: false,
                defaultValue: string.Empty);

            await tableMigrator.ExecuteMigrationOperations(context, tagsMigration);
        }

        if (!await tableMigrator.ColumnExists(context, tableName, nameof(EventEntry.EventSource)))
        {
            logger.AddingEventSourceColumn(tableName);

            var eventSourceMigration = new MigrationBuilder(context.Database.ProviderName);
            eventSourceMigration.AddColumn<string>(
                name: nameof(EventEntry.EventSource),
                table: tableName,
                nullable: true);

            await tableMigrator.ExecuteMigrationOperations(context, eventSourceMigration);
        }
    }
}
