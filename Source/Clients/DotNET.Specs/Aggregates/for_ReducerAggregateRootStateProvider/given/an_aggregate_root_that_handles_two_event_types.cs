// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_ReducerAggregateRootStateProvider.given;

public class an_aggregate_root_that_handles_two_event_types : a_reducer_aggregate_root_state_provider
{
    protected StatefulAggregateRoot _aggregateRoot;
    protected EventSourceId _eventSourceId;
    protected IImmutableList<EventType> _eventTypes;
    protected IAggregateRootContext _aggregateRootContext;

    void Establish()
    {
        _aggregateRoot = new StatefulAggregateRoot();
        _eventSourceId = EventSourceId.New();
        _aggregateRootContext = Substitute.For<IAggregateRootContext>();
        _aggregateRootContext.EventSourceId.Returns(_eventSourceId);
        _aggregateRootContext.AggregateRoot.Returns(_aggregateRoot);
        _aggregateRootContext.EventSequence.Returns(_eventSequence);

        _eventTypes = new EventType[]
        {
            FirstEventType.EventTypeId,
            SecondEventType.EventTypeId
        }.ToImmutableList();

        _reducer.EventTypes.Returns(_eventTypes);

        _provider = new ReducerAggregateRootStateProvider<StateForAggregateRoot>(
            _aggregateRootContext,
            _reducer);
    }
}
