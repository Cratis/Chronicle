// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverHandledCountMigration;

public class when_upgrading_sqlite : Specification, IDisposable
{
    SqliteConnection _connection;
    NamespaceDbContext _context;
    object _countsPerEventType;
    object _count;
    string _identifier;

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
        var migration = new Migrations.v19_110_0 { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await using var read = _connection.CreateCommand();
        read.CommandText = "SELECT HandledEventCountPerEventType FROM Observers WHERE Id = 'legacy-observer'";
        _countsPerEventType = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT HandledEventCount FROM Observers WHERE Id = 'legacy-observer'";
        _count = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT Id FROM Observers";
        _identifier = (string)await read.ExecuteScalarAsync();
    }

    [Fact] void should_leave_legacy_counts_per_event_type_absent() => _countsPerEventType.ShouldEqual(DBNull.Value);
    [Fact] void should_default_the_legacy_handled_event_count_to_zero() => _count.ShouldEqual(0L);
    [Fact] void should_preserve_existing_rows() => _identifier.ShouldEqual("legacy-observer");

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
