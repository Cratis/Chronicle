// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_ProjectionAggregateRootStateProvider.given;

public class an_aggregate_root_that_handles_two_event_types : a_projection_aggregate_root_state_provider
{
    protected StatefulAggregateRoot _aggregateRoot;
    protected EventSourceId _eventSourceId;
    protected IImmutableList<EventType> _eventTypes;
    protected AggregateRootContext _aggregateRootContext;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _aggregateRoot = new StatefulAggregateRoot();
        _eventSourceId = EventSourceId.New();
        _correlationId = CorrelationId.New();
        _aggregateRootContext = new AggregateRootContext(
            _correlationId,
            _eventSourceId,
            _eventSequence,
            _aggregateRoot,
            true,
            false);

        _eventTypes = new EventType[]
        {
            FirstEventType.EventTypeId,
            SecondEventType.EventTypeId
        }.ToImmutableList();

        _provider = new ProjectionAggregateRootStateProvider<StateForAggregateRoot>(
            _aggregateRootContext,
            _projections);
    }
}
