// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Naming.for_ResourceNameReservations;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.for_EventStoreStorage;

public class when_getting_a_namespace_after_it_is_quarantined : Specification, IDisposable
{
    SqliteConnection _connection;
    ResourceNameSnapshot _snapshot;
    EventStoreStorage _storage;
    Exception _error;

    async Task Establish()
    {
        _connection = new SqliteConnection($"DataSource=namespace-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using (var context = new EventStoreDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        var database = Substitute.For<IDatabase>();
        database.EventStore("orders").Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(options), static () => { })));
        _snapshot = ResourceNameSnapshot.NotReady();
        _snapshot.Publish([], []);
        var gate = new ResourceNameGate(_snapshot, new FakeReservations(), ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);
        _storage = new(
            "orders",
            database,
            Substitute.For<IInstancesOf<ISinkFactory>>(),
            Substitute.For<Choreography.Storage.IJobsStorage>(),
            new JsonSerializerOptions(),
            Substitute.For<Json.IExpandoObjectConverter>(),
            null,
            new NamespaceOpeningGuard(gate, () => Task.FromResult<IEnumerable<string>>(["orders"])));
        await _storage.Namespaces.Ensure("tenant");
        _storage.GetNamespace("tenant");
        _snapshot.Publish([], [("orders", "tenant")]);
    }

    void Because() => _error = Catch.Exception(() => _storage.GetNamespace("tenant"));

    [Fact] void should_refuse_it_on_every_call() => _error.ShouldBeOfExactType<ResourceQuarantined>();

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
