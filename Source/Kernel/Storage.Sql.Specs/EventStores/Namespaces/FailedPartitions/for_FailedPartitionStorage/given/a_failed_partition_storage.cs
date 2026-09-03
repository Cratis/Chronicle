// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionStorage.given;

/// <summary>
/// Sets up failed-partition storage backed by a shared in-memory SQLite database.
/// </summary>
public class a_failed_partition_storage : Specification, IDisposable
{
    protected static readonly EventStoreName _eventStore = "test-store";
    protected static readonly EventStoreNamespaceName _namespace = "test-namespace";
    protected SqliteConnection _connection;
    protected IDatabase _database;
    protected FailedPartitionStorage _storage;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var schemaContext = CreateContext())
        {
            schemaContext.Database.EnsureCreated();
        }

        _database = Substitute.For<IDatabase>();
        _database.LiveQueryPollingInterval.Returns(TimeSpan.FromMilliseconds(10));
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => new DbContextScope<NamespaceDbContext>(CreateContext(), () => { }));

        _storage = new FailedPartitionStorage(_eventStore, _namespace, _database);
    }

    protected NamespaceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new NamespaceDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
