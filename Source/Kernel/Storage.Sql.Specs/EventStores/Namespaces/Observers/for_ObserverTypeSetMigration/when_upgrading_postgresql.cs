// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverTypeSetMigration;

[Collection(PostgreSqlCollection.Name)]
public class when_upgrading_postgresql(PostgreSqlFixture fixture) : Specification
{
    NamespaceDbContext _context;
    string _connectionString;
    object _control;
    object _initialization;
    string _initializationColumnType;
    string _identifier;
    string _columnType;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
        builder.UseDatabaseFromConnectionString(_connectionString).AddConceptAsSupport();
        _context = new(builder.Options);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE \"Observers\" (\"Id\" TEXT NOT NULL PRIMARY KEY)");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO \"Observers\" (\"Id\") VALUES ('legacy-observer')");
    }

    async Task Because()
    {
        var migration = new Migrations.v19_79_0 { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await _context.Database.OpenConnectionAsync();
        await using var read = _context.Database.GetDbConnection().CreateCommand();
        read.CommandText = "SELECT \"TypeSetControl\" FROM \"Observers\" WHERE \"Id\" = 'legacy-observer'";
        _control = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT \"Initialization\" FROM \"Observers\" WHERE \"Id\" = 'legacy-observer'";
        _initialization = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT pg_typeof(\"Initialization\")::text FROM \"Observers\"";
        _initializationColumnType = (string)await read.ExecuteScalarAsync();
        read.CommandText = "SELECT \"Id\" FROM \"Observers\"";
        _identifier = (string)await read.ExecuteScalarAsync();
        read.CommandText = "SELECT pg_typeof(\"TypeSetControl\")::text FROM \"Observers\"";
        _columnType = (string)await read.ExecuteScalarAsync();
    }

    [Fact] void should_leave_legacy_control_absent() => _control.ShouldEqual(DBNull.Value);
    [Fact] void should_leave_legacy_initialization_absent() => _initialization.ShouldEqual(DBNull.Value);
    [Fact] void should_store_initialization_as_native_json() => _initializationColumnType.ShouldEqual("jsonb");
    [Fact] void should_bind_initialization_parameters_as_json() => _context.Model.FindEntityType(typeof(ObserverState)).FindProperty(nameof(ObserverState.Initialization)).GetColumnType().ShouldEqual("jsonb");
    [Fact] void should_preserve_existing_rows() => _identifier.ShouldEqual("legacy-observer");
    [Fact] void should_use_native_json_storage() => _columnType.ShouldEqual("jsonb");
    [Fact] void should_bind_control_parameters_as_json() => _context.Model.FindEntityType(typeof(ObserverState)).FindProperty(nameof(ObserverState.TypeSetControl)).GetColumnType().ShouldEqual("jsonb");

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await fixture.DropDatabase(_connectionString);
    }
}
