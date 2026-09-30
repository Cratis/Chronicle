// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.States.for_Observing.when_entering;

public class subscribing_to_stream_for_an_observer_of_all_events : given.an_observing_state
{
    void Establish()
    {
        _observerDefinition = _observerDefinition with { EventTypes = [] };
        _definitionState.State.Returns(_observerDefinition);
        _storedState = _storedState with { NextEventSequenceNumber = 42UL, SubscribesToAllEvents = true };
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_subscribe_to_all_event_types() => _appendedEventsQueues.Received(1).SubscribeToAllEventTypes(_observerKey, Arg.Any<Concepts.Observation.ObserverFilters?>());
    [Fact] void should_not_subscribe_by_event_types() => _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<Concepts.Observation.ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<Concepts.Observation.ObserverFilters?>());
}
