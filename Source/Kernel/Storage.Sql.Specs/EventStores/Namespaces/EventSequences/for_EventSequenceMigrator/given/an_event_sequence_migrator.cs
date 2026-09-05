// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

#pragma warning disable CA2100 // Table names are constants owned by the specs.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.given;

public class an_event_sequence_migrator : Specification
{
    string _connectionString;
    protected SqliteConnection _connection;
    protected EventSequenceMigrator _migrator;
    protected string ConnectionString => _connectionString;

    void Establish()
    {
        var databaseName = $"esm_{Guid.NewGuid():N}";
        _connectionString = $"DataSource={databaseName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        var tableMigrator = new TableMigrator<EventSequenceDbContext>(
            Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>());
        _migrator = new EventSequenceMigrator(
            tableMigrator,
            Substitute.For<ILogger<EventSequenceMigrator>>());
    }

    void Destroy()
    {
        _connection.Close();
        _connection.Dispose();
    }

    protected EventSequenceDbContext CreateContext(string tableName)
    {
        var options = new DbContextOptionsBuilder<EventSequenceDbContext>()
            .UseSqlite(_connectionString)
            .AddConceptAsSupport()
            .Options;

        return new EventSequenceDbContext(options, tableName, _migrator);
    }

    protected async Task Execute(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
