// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage.given;

public class an_unmigrated_closed_streams_database : Specification, IDisposable
{
    protected ClosedStreamsConstraintStorage _storage;
    SqliteConnection _connection;
    string _connectionString;

    void Establish()
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"closed-streams-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        }.ToString();
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => new DbContextScope<NamespaceDbContext>(CreateContext(), () => { }));
        _storage = new("test-store", "test-namespace", EventSequenceId.Log, database);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    protected NamespaceDbContext CreateContext() => new(new DbContextOptionsBuilder<NamespaceDbContext>()
        .UseSqlite(_connectionString).AddConceptAsSupport().Options);
}
