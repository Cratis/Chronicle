// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_live_progress_advances_during_setup : given.an_observer_with_subscription
{
    readonly TaskCompletionSource _resolving = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<IEnumerable<EventTypeSchema>> _schemas = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
    {
        _resolving.TrySetResult();
        return _schemas.Task;
    });

    async Task Because()
    {
        var reconciliation = _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero, false);
        await _resolving.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _observer.CaughtUp(JobId.NotSet, 84UL);
        _schemas.TrySetResult([]);
        await reconciliation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_keep_the_live_cursor() => (await _observer.GetState()).NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)85UL);
    [Fact] void should_not_reload_progress() => _storageStats.Reads.ShouldEqual(0);
}
