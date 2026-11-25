// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverDurableSubscriptionMigration;

public class when_adding_catch_up_repairs : Specification
{
    SqliteConnection _connection;
    NamespaceDbContext _context;
    string _legacyId;
    object _legacyRepairs;
    bool _nullable;

    async Task Establish()
    {
        _connection = new("Data Source=:memory:");
        await _connection.OpenAsync();
        _context = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connection).Options);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE Observers (Id TEXT PRIMARY KEY);");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO Observers VALUES ('existing');");
    }

    async Task Because()
    {
        var migration = new v19_173_0_2 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" };
        _nullable = migration.UpOperations.OfType<AddColumnOperation>().Single().IsNullable;
        var generator = _context.GetService<IMigrationsSqlGenerator>();
        foreach (var command in generator.Generate(migration.UpOperations, _context.Model))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await using var query = _connection.CreateCommand();
        query.CommandText = "SELECT Id, PendingCatchUpRepairs FROM Observers";
        await using var reader = await query.ExecuteReaderAsync();
        await reader.ReadAsync();
        _legacyId = reader.GetString(0);
        _legacyRepairs = reader.GetValue(1);
    }

    [Fact] void should_keep_the_existing_row() => _legacyId.ShouldEqual("existing");
    [Fact] void should_not_backfill_a_legacy_obligation() => _legacyRepairs.ShouldEqual(DBNull.Value);
    [Fact] void should_add_a_nullable_column() => _nullable.ShouldBeTrue();

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
