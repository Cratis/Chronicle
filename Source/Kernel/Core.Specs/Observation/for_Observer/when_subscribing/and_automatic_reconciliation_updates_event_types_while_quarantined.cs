// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_automatic_reconciliation_updates_event_types_while_quarantined : given.a_quarantined_observer
{
    readonly EventType[] _eventTypes = [new("079e1a45-6461-4de5-a5e1-ed2fa15c57f6", EventTypeGeneration.First)];

    async Task Because() => await _observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(
        ObserverType.External,
        _eventTypes,
        SiloAddress.Zero,
        "target",
        automatic: true);

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_record_the_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_update_subscription_event_types() => (await _observer.GetSubscription()).EventTypes.ShouldEqual(_eventTypes);
    [Fact] void should_update_definition_event_types() => _definitionStorage.State.EventTypes.ShouldEqual(_eventTypes);
    [Fact] void should_not_start_catchup() => ShouldNotHaveStartedCatchup();
    [Fact] void should_not_start_replay() => ShouldNotHaveStartedReplay();
    [Fact] void should_not_resubscribe_to_the_queue() => ShouldNotHaveResubscribed();
}
