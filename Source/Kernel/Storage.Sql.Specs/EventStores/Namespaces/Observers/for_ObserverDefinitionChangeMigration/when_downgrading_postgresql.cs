// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverDefinitionChangeMigration;

[Collection(PostgreSqlCollection.Name)]
public class when_downgrading_postgresql(PostgreSqlFixture fixture) : Specification
{
    NamespaceDbContext _context;
    string _connectionString;
    Migrations.v19_79_1 _migration;
    long _tables;
    string _legacy;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _context = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseNpgsql(_connectionString).AddConceptAsSupport().Options);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE \"Observers\" (\"Id\" TEXT NOT NULL PRIMARY KEY)");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO \"Observers\" (\"Id\") VALUES ('legacy-observer')");
        _migration = new() { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(_migration.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
    }

    async Task Because()
    {
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(_migration.DownOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await _context.Database.OpenConnectionAsync();
        await using var read = _context.Database.GetDbConnection().CreateCommand();
        read.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'ObserverDefinitionChangeRows'";
        _tables = (long)(await read.ExecuteScalarAsync())!;
        read.CommandText = "SELECT \"Id\" FROM \"Observers\"";
        _legacy = (string)await read.ExecuteScalarAsync();
    }

    [Fact] void should_drop_the_row_table() => _tables.ShouldEqual(0L);
    [Fact] void should_leave_legacy_observers_unchanged() => _legacy.ShouldEqual("legacy-observer");

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await fixture.DropDatabase(_connectionString);
    }
}
