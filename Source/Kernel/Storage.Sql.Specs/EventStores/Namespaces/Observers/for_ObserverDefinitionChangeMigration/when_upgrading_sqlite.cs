// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverDefinitionChangeMigration;

public class when_upgrading_sqlite : Specification, IDisposable
{
    SqliteConnection _connection;
    NamespaceDbContext _context;
    long _rowCount;
    string _legacy;
    string _positionType;

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
        var migration = new Migrations.v19_79_1 { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await using var read = _connection.CreateCommand();
        read.CommandText = "SELECT COUNT(*) FROM ObserverDefinitionChangeRows";
        _rowCount = (long)(await read.ExecuteScalarAsync())!;
        read.CommandText = "SELECT Id FROM Observers";
        _legacy = (string)await read.ExecuteScalarAsync();
        read.CommandText = "SELECT type FROM pragma_table_info('ObserverDefinitionChangeRows') WHERE name = 'Position'";
        _positionType = (string)await read.ExecuteScalarAsync();
    }

    [Fact] void should_create_empty_row_storage() => _rowCount.ShouldEqual(0L);
    [Fact] void should_leave_legacy_observers_unchanged() => _legacy.ShouldEqual("legacy-observer");
    [Fact] void should_preserve_unsigned_positions_in_text_storage() => _positionType.ShouldEqual("TEXT");

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
