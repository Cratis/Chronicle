// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverDefinitionChangeMigration;

[Collection(PostgreSqlCollection.Name)]
public class when_upgrading_postgresql(PostgreSqlFixture fixture) : Specification
{
    NamespaceDbContext _context;
    string _connectionString;
    long _rowCount;
    string _legacy;
    string _jsonType;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _context = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseNpgsql(_connectionString).AddConceptAsSupport().Options);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE \"Observers\" (\"Id\" TEXT NOT NULL PRIMARY KEY)");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO \"Observers\" (\"Id\") VALUES ('legacy-observer')");
    }

    async Task Because()
    {
        var migration = new Migrations.v19_79_1 { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await _context.Database.OpenConnectionAsync();
        await using var read = _context.Database.GetDbConnection().CreateCommand();
        read.CommandText = "SELECT COUNT(*) FROM \"ObserverDefinitionChangeRows\"";
        _rowCount = (long)(await read.ExecuteScalarAsync())!;
        read.CommandText = "SELECT \"Id\" FROM \"Observers\"";
        _legacy = (string)await read.ExecuteScalarAsync();
        read.CommandText = "SELECT udt_name FROM information_schema.columns WHERE table_name = 'ObserverDefinitionChangeRows' AND column_name = 'Data'";
        _jsonType = (string)await read.ExecuteScalarAsync();
    }

    [Fact] void should_create_empty_row_storage() => _rowCount.ShouldEqual(0L);
    [Fact] void should_leave_legacy_observers_unchanged() => _legacy.ShouldEqual("legacy-observer");
    [Fact] void should_use_native_json_storage() => _jsonType.ShouldEqual("jsonb");
    [Fact] void should_bind_json_parameters_natively() => _context.Model.FindEntityType(typeof(ObserverDefinitionChangeEntry)).FindProperty(nameof(ObserverDefinitionChangeEntry.Data)).GetColumnType().ShouldEqual("jsonb");

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await fixture.DropDatabase(_connectionString);
    }
}
