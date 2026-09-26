// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.given;

/// <summary>Real SQLite migration against an isolated namespace database.</summary>
public class a_named_tag_migrator : Specification, IDisposable
{
    protected SqliteConnection _connection;
    string _connectionString;
    EventSequenceMigrator _migrator;

    void Establish()
    {
        _connectionString = $"DataSource=nt_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
        _migrator = new EventSequenceMigrator(
            new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>()),
            Substitute.For<ILogger<EventSequenceMigrator>>());
    }

    protected EventSequenceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventSequenceDbContext>()
            .UseSqlite(_connectionString)
            .AddConceptAsSupport()
            .Options;
        return new EventSequenceDbContext(options, "event-sequence", _migrator);
    }

    protected async Task Execute(string sql)
    {
        await using var command = _connection.CreateCommand();
#pragma warning disable CA2100 // Only hard-coded schema fixtures call this test helper.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    protected async Task<bool> Exists(string name)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        command.Parameters.AddWithValue("$name", name);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
