// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.given;

/// <summary>
/// An isolated SQL Server database with event sequence tables that can be opened by fresh migrators.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
public abstract class a_sql_server_sequence_database(SqlServerFixture fixture) : Specification
{
    protected const string Table = "event-sequence";
    protected string _connectionString;
    protected a_captured_migrator_log _log;
    protected EventSequenceMigrator _migrator;

    protected async Task Execute(string sql)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Only hard-coded fixtures call this test helper.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    protected async Task<long[]> ReviewCounts(string table)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Hard-coded fixture query.
        command.CommandText = $"SELECT EventSourceIds, EventStreamIds FROM [{EventSequenceMigrator.SpellingReviewsTable}] WHERE TableName = '{table}'";
#pragma warning restore CA2100
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? [reader.GetInt64(0), reader.GetInt64(1)] : [];
    }

    protected async Task<bool> ReviewTableExists()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{EventSequenceMigrator.SpellingReviewsTable}'";
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    protected Task Seed(string table, string eventSourceId, string streamType, string streamId, int sequenceNumber) =>
        Execute($"INSERT INTO [{table}] (SequenceNumber, CorrelationId, Causation, CausedBy, Type, Occurred, EventSourceType, EventSourceId, EventStreamType, EventStreamId, Content, ContentHashes, Compensations, Tags) VALUES ({sequenceNumber}, N'', N'[]', N'', N'type', SYSDATETIMEOFFSET(), N'', N'{eventSourceId}', N'{streamType}', N'{streamId}', N'{{}}', N'{{}}', N'{{}}', N'[]')");

    /// <summary>
    /// Starts a fresh migrator, with a fresh log, that has not yet opened any table in this process.
    /// </summary>
    protected void StartMigrator()
    {
        _log = new();
        _migrator = new(new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance), _log);
        _migrator.ClearMigrationCache(_connectionString);
    }

    protected EventSequenceDbContext CreateContext(string table = Table)
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>().UseSqlServer(_connectionString);
        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        return new(builder.Options, table, _migrator);
    }

    protected async Task CreateTable(string table = Table)
    {
        await using var context = CreateContext(table);
        await context.EnsureTableExists();
    }

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        StartMigrator();
    }

    async Task Destroy() => await fixture.DropDatabase(_connectionString);
}
