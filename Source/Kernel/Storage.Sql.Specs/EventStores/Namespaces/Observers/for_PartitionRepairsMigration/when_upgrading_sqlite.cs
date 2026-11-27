// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_PartitionRepairsMigration;

public class when_upgrading_sqlite : Specification, IDisposable
{
    SqliteConnection _connection;
    NamespaceDbContext _context;
    object _partitionRepairs;
    string _identifier;
    bool _columnExists;
    long _cursor;

    async Task Establish()
    {
        _connection = new("DataSource=:memory:");
        await _connection.OpenAsync();
        _context = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE Observers (Id TEXT NOT NULL PRIMARY KEY, NextEventSequenceNumber INTEGER)");
        var previous = new Migrations.v19_173_0_1 { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(previous.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO Observers (Id, NextEventSequenceNumber) VALUES ('legacy-observer', 42)");
    }

    async Task Because()
    {
        var migration = new Migrations.v19_174_0_1 { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await using var read = _connection.CreateCommand();
        read.CommandText = "SELECT PartitionRepairs FROM Observers WHERE Id = 'legacy-observer'";
        _partitionRepairs = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT Id FROM Observers";
        _identifier = (string)await read.ExecuteScalarAsync();
        read.CommandText = "SELECT NextEventSequenceNumber FROM Observers";
        _cursor = (long)(await read.ExecuteScalarAsync())!;
        read.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Observers') WHERE name = 'PartitionRepairs' AND \"notnull\" = 0";
        _columnExists = await read.ExecuteScalarAsync() is long count && count == 1;
    }

    [Fact] void should_add_a_nullable_column() => _columnExists.ShouldBeTrue();
    [Fact] void should_leave_the_partition_repairs_of_a_legacy_row_absent() => _partitionRepairs.ShouldEqual(DBNull.Value);
    [Fact] void should_preserve_the_cursor() => _cursor.ShouldEqual(42L);
    [Fact] void should_preserve_existing_rows() => _identifier.ShouldEqual("legacy-observer");

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
