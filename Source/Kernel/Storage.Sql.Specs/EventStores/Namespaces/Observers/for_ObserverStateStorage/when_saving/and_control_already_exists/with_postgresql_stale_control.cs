// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_saving.and_control_already_exists;

[Collection(PostgreSqlCollection.Name)]
public class with_postgresql_stale_control(PostgreSqlFixture fixture) : Specification
{
    static readonly EventTypeId _type = new("event-a");
    string _connectionString;
    ObserverStateStorage _storage;
    NamespaceDbContext _delayedContext;
    ObserverTypeSetControl _control;
    KernelObserverState _saved;
    Exception _error;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        _storage = CreateStorage();
        _control = new(
            new(5),
            new("reactor-subscriber", ObserverFilters.None),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(5), new(42)) },
            []);
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = _control });
    }

    async Task Because()
    {
        var barrier = new given.ControlWriteBarrier();
        var delayedStorage = CreateStorage(barrier);
        var oldSave = delayedStorage.Save(new() { Identifier = "observer", TypeSetControl = _control with { Revision = new(6) } });
        try
        {
            await barrier.Reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var current = _control with
            {
                Revision = new(7),
                Members = new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(7), new(123)) }
            };
            await _storage.Save(new() { Identifier = "observer", TypeSetControl = current });
        }
        finally
        {
            barrier.Continue.TrySetResult();
        }

        _error = await Catch.Exception(() => oldSave);
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = _control, NextEventSequenceNumber = new(456) });
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_dispose_the_delayed_context_after_the_failed_save() => Catch.Exception(() => _ = _delayedContext.Model).ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_fail_the_in_flight_snapshot() => _error.ShouldBeOfExactType<DbUpdateConcurrencyException>();
    [Fact] void should_preserve_the_newer_control() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_preserve_the_rejoined_incarnation() => _saved.TypeSetControl.Members[_type].Incarnation.Value.ShouldEqual(7);
    [Fact] void should_preserve_the_new_join_fence() => _saved.TypeSetControl.Members[_type].JoinFence.Value.ShouldEqual(123UL);
    [Fact] void should_save_other_fields_with_the_stale_control() => _saved.NextEventSequenceNumber.Value.ShouldEqual(456UL);

    ObserverStateStorage CreateStorage(given.ControlWriteBarrier? barrier = null)
    {
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(barrier), () => { })));
        return new(EventStoreName.System, EventStoreNamespaceName.Default, database);
    }

    NamespaceDbContext CreateContext(given.ControlWriteBarrier? barrier = null)
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
        builder.UseDatabaseFromConnectionString(_connectionString).AddConceptAsSupport();
        if (barrier is not null)
        {
            builder.AddInterceptors(barrier).EnableServiceProviderCaching(false);
        }

        var context = new NamespaceDbContext(builder.Options);
        if (barrier is not null)
        {
            _delayedContext = context;
        }

        return context;
    }

    async Task Destroy() => await fixture.DropDatabase(_connectionString);
}
