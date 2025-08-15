// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Observation.States.for_Routing.given;

public class a_routing_state_and_event_types_that_are_different_from_subscription : a_routing_state
{
    void Establish()
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
            Tail = EventSequenceNumber.Unavailable,
            TailForEventTypes = EventSequenceNumber.Unavailable
        };
    }
}
