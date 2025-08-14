// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Observation.States.for_Routing;

public class when_leaving : given.a_routing_state
{
    async void Establish()
    {
        _subscription = _subscription with
        {
            EventTypes =
            [
                new EventType("e433be87-2d05-49b1-b093-f0cec977429b", EventTypeGeneration.First)
            ]
        };

        _tailEventSequenceNumbers = _tailEventSequenceNumbers with
        {
            Tail = 42L,
            TailForEventTypes = 21L
        };

        _storedState = await _state.OnEnter(_storedState);
    }

    async Task Because() => await _state.OnLeave(_storedState);

    [Fact] void should_set_event_types_to_subscribers_event_types() => _definitionState.State.EventTypes.ShouldEqual(_subscription.EventTypes);
}
