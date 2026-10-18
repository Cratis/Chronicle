// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverTypeSetMigration;

public class when_upgrading_sqlite : Specification, IDisposable
{
    SqliteConnection _connection;
    NamespaceDbContext _context;
    object _control;
    object _initialization;
    string _identifier;

    async Task Establish()
    {
        _connection = new("DataSource=:memory:");
        await _connection.OpenAsync();
        _context = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connection).AddConceptAsSupport().Options);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE Observers (Id TEXT NOT NULL PRIMARY KEY)");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO Observers (Id) VALUES ('legacy-observer')");
    }

    async Task Because()
    {
        var migration = new Migrations.v19_83_1_0 { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await using var read = _connection.CreateCommand();
        read.CommandText = "SELECT TypeSetControl FROM Observers WHERE Id = 'legacy-observer'";
        _control = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT Initialization FROM Observers WHERE Id = 'legacy-observer'";
        _initialization = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT Id FROM Observers";
        _identifier = (string)await read.ExecuteScalarAsync();
    }

    [Fact] void should_leave_legacy_control_absent() => _control.ShouldEqual(DBNull.Value);
    [Fact] void should_leave_legacy_initialization_absent() => _initialization.ShouldEqual(DBNull.Value);
    [Fact] void should_preserve_existing_rows() => _identifier.ShouldEqual("legacy-observer");

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
