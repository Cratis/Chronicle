// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CA2100 // Specification-owned identifiers.

namespace Cratis.Chronicle.Storage.Sql.for_TableMigrator.when_checking_a_hostile_table_name.given;

/// <summary>
/// A real <see cref="TableMigrator{TContext}"/> over a named shared-cache in-memory SQLite database holding one
/// table named "canary" that no read model asks for.
/// </summary>
public class a_sqlite_table_migrator_with_a_canary : Specification
{
    protected const string Canary = "canary";

    protected TableMigrator<ReadModelDbContext> _migrator;
    protected int _creations;

    string _connectionString;
    SqliteConnection _masterConnection;

    void Establish()
    {
        // A database of its own per spec, so the migrator's static caches are isolated between runs.
        _connectionString = $"DataSource=tm_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _masterConnection = new SqliteConnection(_connectionString);
        _masterConnection.Open();
        using var command = _masterConnection.CreateCommand();
        command.CommandText = "CREATE TABLE \"canary\" (\"Id\" integer NOT NULL)";
        command.ExecuteNonQuery();
        _migrator = new(NullLogger<TableMigrator<ReadModelDbContext>>.Instance);
    }

    void Destroy()
    {
        _masterConnection.Close();
        _masterConnection.Dispose();
    }

    protected ReadModelDbContext CreateContext(string tableName)
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(_connectionString)
            .AddConceptAsSupport().WithoutServiceProviderCaching()
            .Options;
        return new ReadModelDbContext(options, tableName, [new ProjectedColumn("Id", typeof(int), IsKey: true, IsJson: false, IsArray: false, IsNullable: false)], Substitute.For<IReadModelMigrator>());
    }

    protected Task RecordCreation(ReadModelDbContext context, string tableName)
    {
        _creations++;
        return Task.CompletedTask;
    }
}
