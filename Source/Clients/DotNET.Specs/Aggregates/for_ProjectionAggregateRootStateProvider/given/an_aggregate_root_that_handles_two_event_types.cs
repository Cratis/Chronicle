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
    protected IAggregateRootContext _aggregateRootContext;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _aggregateRoot = new StatefulAggregateRoot();
        _eventSourceId = EventSourceId.New();
        _correlationId = CorrelationId.New();
        _aggregateRootContext = Substitute.For<IAggregateRootContext>();
        _aggregateRootContext.CorrelationId.Returns(_correlationId);
        _aggregateRootContext.EventSourceId.Returns(_eventSourceId);
        _aggregateRootContext.AggregateRoot.Returns(_aggregateRoot);
        _aggregateRootContext.EventSequence.Returns(_eventSequence);

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
