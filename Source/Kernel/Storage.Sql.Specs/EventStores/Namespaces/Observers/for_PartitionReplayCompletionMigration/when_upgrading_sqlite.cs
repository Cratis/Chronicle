// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_PartitionReplayCompletionMigration;

public class when_upgrading_sqlite : Specification, IDisposable
{
    SqliteConnection _connection;
    NamespaceDbContext _context;
    object _completion;
    object _identifier;

    async Task Establish()
    {
        _connection = new("DataSource=:memory:");
        await _connection.OpenAsync();
        _context = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE Observers (Id TEXT NOT NULL PRIMARY KEY)");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO Observers (Id) VALUES ('legacy-observer')");
    }

    async Task Because()
    {
        var migration = new Migrations.v19_111_0_0 { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        await using var read = _connection.CreateCommand();
        read.CommandText = "SELECT PartitionReplayCompletions FROM Observers WHERE Id = 'legacy-observer'";
        _completion = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT Id FROM Observers";
        _identifier = await read.ExecuteScalarAsync();
    }

    [Fact] void should_leave_the_new_column_null_for_legacy_rows() => _completion.ShouldEqual(DBNull.Value);
    [Fact] void should_preserve_existing_rows() => _identifier.ShouldEqual("legacy-observer");

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
