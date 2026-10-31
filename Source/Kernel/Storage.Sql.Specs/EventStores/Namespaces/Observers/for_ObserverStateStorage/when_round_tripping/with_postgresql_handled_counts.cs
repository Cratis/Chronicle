// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

[Collection(PostgreSqlCollection.Name)]
public class with_postgresql_handled_counts(PostgreSqlFixture fixture) : Specification
{
    string _connectionString;
    ObserverStateStorage _storage;
    KernelObserverState _saved;
    string _columnType;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), () => { })));
        _storage = new(EventStoreName.System, EventStoreNamespaceName.Default, database);
    }

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", HandledEventCount = 7, HandledEventCountPerEventType = new Dictionary<EventTypeId, EventCount> { ["event-a"] = 4, ["event-b"] = 3 } });
        _saved = await _storage.Get("observer");
        await using var context = CreateContext();
        await context.Database.OpenConnectionAsync();
        await using var read = context.Database.GetDbConnection().CreateCommand();
        read.CommandText = "SELECT data_type FROM information_schema.columns WHERE table_name = 'Observers' AND column_name = 'HandledEventCountPerEventType'";
        _columnType = (string)await read.ExecuteScalarAsync();
    }

    [Fact] void should_keep_the_handled_event_count() => _saved.HandledEventCount.Value.ShouldEqual(7UL);
    [Fact] void should_keep_the_count_for_each_event_type() => _saved.HandledEventCountPerEventType.ShouldContainOnly(new Dictionary<EventTypeId, EventCount> { ["event-a"] = 4, ["event-b"] = 3 });
    [Fact] void should_keep_only_the_saved_event_types() => _saved.HandledEventCountPerEventType.Count.ShouldEqual(2);
    [Fact] void should_store_counts_per_event_type_as_jsonb() => _columnType.ShouldEqual("jsonb");

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    NamespaceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
        builder.UseDatabaseFromConnectionString(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching();
        return new(builder.Options);
    }
}
