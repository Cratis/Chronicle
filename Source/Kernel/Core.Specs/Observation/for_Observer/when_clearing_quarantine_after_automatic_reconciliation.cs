// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_quarantine_after_automatic_reconciliation : given.a_quarantined_observer
{
    readonly EventType[] _eventTypes = [new("079e1a45-6461-4de5-a5e1-ed2fa15c57f6", EventTypeGeneration.First)];

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { Identifier = _observerId };
        await _observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(
            ObserverType.External,
            _eventTypes,
            SiloAddress.Zero,
            "target",
            automatic: true);
    }

    async Task Because() => await _observer.ClearObserverQuarantine();

    [Fact] void should_resume_observing() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_subscribe_to_the_updated_event_types() => _appendedEventsQueues.Received(1)
        .Subscribe(_observerKey, Arg.Is<IEnumerable<EventType>>(types => types.SequenceEqual(_eventTypes)), Arg.Any<ObserverFilters?>());
    [Fact] async Task should_keep_the_updated_definition() => (await _observer.GetEventTypes()).ShouldEqual(_eventTypes);
}
