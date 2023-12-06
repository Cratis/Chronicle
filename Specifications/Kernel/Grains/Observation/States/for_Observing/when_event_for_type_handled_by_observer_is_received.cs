// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Observing;

public class when_event_for_type_handled_by_observer_is_received : given.an_observing_state
{
    AppendedEvent event_published;
    EventType event_type;
    EventSourceId event_source_id;

    async Task Establish()
    {
        event_type = new(Guid.NewGuid(), EventGeneration.First);
        stored_state = stored_state with { EventTypes = new[] { event_type } };

        event_published = AppendedEvent.EmptyWithEventType(event_type);

        event_source_id = Guid.NewGuid().ToString();
        event_published = event_published with
        {
            Context = event_published.Context with
            {
                EventSourceId = event_source_id
            }
        };

        await state.OnEnter(stored_state);
    }

    async Task Because() => await observed_stream.OnNextAsync(event_published);

    [Fact] void should_call_observer_with_event() => observer.Verify(_ => _.Handle(event_source_id, Is<IEnumerable<AppendedEvent>>(_ => _.Count() == 1 && _.First() == event_published)), Once());
}
