// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.for_AlertIncidentsMigrations.given;

/// <summary>
/// Sets up a named shared-cache in-memory SQLite database with no migrations applied.
/// </summary>
public class an_unmigrated_namespace_database : Specification, IDisposable
{
    SqliteConnection _connection;
    string _connectionString;

    void Establish()
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"alerts-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    protected NamespaceDbContext CreateContext() => new(new DbContextOptionsBuilder<NamespaceDbContext>()
        .UseSqlite(_connectionString)
        .AddConceptAsSupport()
        .Options);
}
