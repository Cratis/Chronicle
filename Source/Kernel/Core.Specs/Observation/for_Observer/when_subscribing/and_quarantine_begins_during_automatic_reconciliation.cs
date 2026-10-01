// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantine_begins_during_automatic_reconciliation : given.an_observer_quarantined_during_a_probe
{
    readonly TaskCompletionSource<IEnumerable<EventTypeSchema>> _query = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
    {
        _probeEntered.TrySetResult();
        return _query.Task;
    });

    async Task Because() => await QuarantineDuringProbe(
        _observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(ObserverType.External, [EventType.Unknown], SiloAddress.Zero, "target", automatic: true),
        () => _query.SetResult([]));

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_record_the_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
}
