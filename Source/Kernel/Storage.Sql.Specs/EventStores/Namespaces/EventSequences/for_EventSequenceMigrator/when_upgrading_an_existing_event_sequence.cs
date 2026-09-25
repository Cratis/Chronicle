// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

#pragma warning disable CA2100 // Table name is generated locally for this specification, never supplied by a caller.

public class when_upgrading_an_existing_event_sequence : Specification, IDisposable
{
    readonly string _tableName = $"events_{Guid.NewGuid():N}";
    SqliteConnection _anchor;
    SqliteConnection _connection;
    bool _hasGeneration;
    bool _legacyGenerationIsNull;

    async Task Establish()
    {
        var connectionString = $"Data Source={_tableName};Mode=Memory;Cache=Shared";
        _anchor = new SqliteConnection(connectionString);
        await _anchor.OpenAsync();
        _connection = new SqliteConnection(connectionString);
        await _connection.OpenAsync();
        await using var command = _anchor.CreateCommand();
        command.CommandText = $"CREATE TABLE \"{_tableName}\" (\"SequenceNumber\" INTEGER PRIMARY KEY, \"Tags\" TEXT); INSERT INTO \"{_tableName}\" (\"SequenceNumber\", \"Tags\") VALUES (0, '');";
        await command.ExecuteNonQueryAsync();
    }

    async Task Because()
    {
        var options = new DbContextOptionsBuilder<EventSequenceDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;
        var tableMigrator = new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>());
        var migrator = new EventSequenceMigrator(tableMigrator, Substitute.For<ILogger<EventSequenceMigrator>>());
        await using var context = new EventSequenceDbContext(options, _tableName, migrator);
        await context.EnsureTableExists();
        _hasGeneration = await tableMigrator.ColumnExists(context, _tableName, nameof(EventEntry.Generation));
        await using var command = _anchor.CreateCommand();
        command.CommandText = $"SELECT \"Generation\" FROM \"{_tableName}\" WHERE \"SequenceNumber\" = 0";
        _legacyGenerationIsNull = await command.ExecuteScalarAsync() is DBNull;
    }

    [Fact] void should_add_the_nullable_generation_column() => _hasGeneration.ShouldBeTrue();
    [Fact] void should_leave_existing_event_generations_unknown() => _legacyGenerationIsNull.ShouldBeTrue();

    public void Dispose()
    {
        _connection?.Dispose();
        _anchor?.Dispose();
        GC.SuppressFinalize(this);
    }
}
