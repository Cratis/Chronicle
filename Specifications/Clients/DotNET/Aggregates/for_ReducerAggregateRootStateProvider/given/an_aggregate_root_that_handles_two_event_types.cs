// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Aggregates.for_ReducerAggregateRootStateProvider.given;

public class an_aggregate_root_that_handles_two_event_types : a_reducer_aggregate_root_state_provider
{
    protected StatelessAggregateRoot aggregate_root;
    protected EventSourceId event_source_id;
    protected IImmutableList<EventType> event_types;

    void Establish()
    {
        aggregate_root = new StatelessAggregateRoot();

        event_source_id = EventSourceId.New();
        aggregate_root.EventSequence = event_sequence.Object;
        aggregate_root.EventHandlers = event_handlers.Object;
        aggregate_root._eventSourceId = event_source_id;

        event_types = new EventType[]
        {
                FirstEventType.EventTypeId,
                SecondEventType.EventTypeId
        }.ToImmutableList();

        event_handlers.SetupGet(_ => _.EventTypes).Returns(event_types);

        provider = new ReducerAggregateRootStateProvider(
            aggregate_root,
            reducer.Object);
    }
}
