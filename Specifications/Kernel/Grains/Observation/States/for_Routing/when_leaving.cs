// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Routing;

public class when_leaving : given.a_routing_state
{
    async void Establish()
    {
        stored_state = stored_state with
        {
            EventTypes = new[]
            {
                new EventType("31252720-dcbb-47ae-927d-26070f7ef8ae", EventGeneration.First)
            }
        };
        subscription = subscription with
        {
            EventTypes = new[]
            {
                new EventType("e433be87-2d05-49b1-b093-f0cec977429b", EventGeneration.First)
            }
        };

        tail_event_sequence_numbers = tail_event_sequence_numbers with
        {
            Tail = 42L,
            TailForEventTypes = 21L
        };

        stored_state = await state.OnEnter(stored_state);
    }

    async Task Because() => resulting_stored_state = await state.OnLeave(stored_state);

    [Fact] void should_set_next_event_sequence_number_to_next_after_tail() => resulting_stored_state.NextEventSequenceNumber.ShouldEqual(tail_event_sequence_numbers.Tail.Next());
    [Fact] void should_set_next_event_sequence_number_for_event_types_to_next_after_tail_for_event_types() => resulting_stored_state.NextEventSequenceNumberForEventTypes.ShouldEqual(tail_event_sequence_numbers.TailForEventTypes.Next());
    [Fact] void should_set_event_types_to_subscribers_event_types() => resulting_stored_state.EventTypes.ShouldEqual(subscription.EventTypes);
}
