// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Observation.States.for_Routing.given;

public class a_routing_state_and_event_types_that_are_different_from_subscription : a_routing_state
{
    void Establish()
    {
        stored_state = stored_state with
        {
            EventTypes =
            [
                new EventType("31252720-dcbb-47ae-927d-26070f7ef8ae", EventTypeGeneration.First)
            ]
        };
        subscription = subscription with
        {
            EventTypes =
            [
                new EventType("e433be87-2d05-49b1-b093-f0cec977429b", EventTypeGeneration.First)
            ]
        };

        tail_event_sequence_numbers = tail_event_sequence_numbers with
        {
            Tail = EventSequenceNumber.Unavailable,
            TailForEventTypes = EventSequenceNumber.Unavailable
        };
    }
}
