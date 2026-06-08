// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_observing_all;

/// <summary>
/// Subscribes to <see cref="ObserverStateStorage.ObserveAll"/> and holds the asynchronous load open
/// until after the subscription is attached. The observable must still deliver the current state to
/// the subscriber once the load completes, so an observer that subscribes before the first query
/// returns never misses the snapshot. This guards against the regression where the observers list
/// stayed permanently empty on the SQL backend because the initial state was never delivered.
/// </summary>
public class and_states_exist : given.an_observer_state_storage
{
    static readonly ObserverId _observerId = new("test-observer");
    IEnumerable<KernelObserverState> _received;
    IDisposable _subscription;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.Observers.Add(new ObserverState { Id = _observerId });
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        // Hold the asynchronous load open so the subscription is guaranteed to attach before the
        // snapshot is published — this is the exact ordering that previously lost the snapshot.
        _load = new TaskCompletionSource();

        var completion = new TaskCompletionSource<IEnumerable<KernelObserverState>>();
        var subject = _storage.ObserveAll();
        _subscription = subject.Subscribe(states => completion.TrySetResult(states));

        // Now that a subscriber is attached, let the load complete and publish the snapshot.
        _load.SetResult();

        _received = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_deliver_the_existing_state_to_the_subscriber() => _received.Count().ShouldEqual(1);
    [Fact] void should_deliver_the_correct_observer() => _received.Single().Identifier.ShouldEqual(_observerId);

    void Destroy() => _subscription?.Dispose();
}
