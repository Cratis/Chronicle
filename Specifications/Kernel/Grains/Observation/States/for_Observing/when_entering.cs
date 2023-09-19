// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.EventSequences;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Observing;

public class when_entering : given.an_observing_state
{
    void Establish() => stored_state.NextEventSequenceNumber = 42UL;

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_subscribe_to_stream() => stream.Verify(_ => _.SubscribeAsync(IsAny<IAsyncObserver<AppendedEvent>>(), new EventSequenceNumberToken(stored_state.NextEventSequenceNumber), null), Once());
}


public class when_event_for_type_handled_by_observer_is_received : given.an_observing_state
{
    AppendedEvent event_published;
    EventType event_type;
    EventSourceId event_source_id;

    async Task Establish()
    {
        event_type = new(Guid.NewGuid(), EventGeneration.First);
        stored_state.EventTypes = new[] { event_type };

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

    async Task Because() => await observer.OnNextAsync(event_published);

    [Fact] void should_call_supervisor_with_event() => observer_supervisor.Verify(_ => _.Handle(event_source_id, Is<IEnumerable<AppendedEvent>>(_ => _.Count() == 1 && _.First() == event_published)), Once());
}
