// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_saving.and_control_already_exists;

public class with_an_in_flight_older_write : given.an_observer_state_storage
{
    ObserverTypeSetControl _control;
    ObserverStateStorage _delayedStorage;
    NamespaceDbContext _delayedContext;
    given.ControlWriteBarrier _barrier;
    Exception _error;
    KernelObserverState _saved;

    async Task Establish()
    {
        _control = new(new(5), new("reactor-subscriber", ObserverFilters.None), new Dictionary<EventTypeId, ObserverTypeSetMember>(), []);
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = _control }, Concepts.EventSequences.EventSequenceId.Log);
        _barrier = new();
        var options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().AddInterceptors(_barrier).Options;
        _delayedContext = new(options);
        var delayedDatabase = Substitute.For<IDatabase>();
        delayedDatabase.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(_delayedContext, () => { })));
        _delayedStorage = new(_eventStore, _namespace, delayedDatabase);
    }

    async Task Because()
    {
        var oldSave = _delayedStorage.Save(new() { Identifier = "observer", TypeSetControl = _control with { Revision = new(6) } }, Concepts.EventSequences.EventSequenceId.Log);
        try
        {
            await _barrier.Reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await _storage.Save(new() { Identifier = "observer", TypeSetControl = _control with { Revision = new(7) } }, Concepts.EventSequences.EventSequenceId.Log);
        }
        finally
        {
            _barrier.Continue.TrySetResult();
        }

        _error = await Catch.Exception(() => oldSave);
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_fail_the_competing_snapshot_instead_of_overwriting_it() => _error.ShouldBeOfExactType<DbUpdateConcurrencyException>();
    [Fact] void should_preserve_the_newer_control() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_dispose_the_delayed_context_after_the_failed_save() => Catch.Exception(() => _ = _delayedContext.Model).ShouldBeOfExactType<ObjectDisposedException>();
}
