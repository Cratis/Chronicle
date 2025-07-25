// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates.for_ReducerAggregateRootStateProvider.given;

public class an_aggregate_root_that_handles_two_event_types : a_reducer_aggregate_root_state_provider
{
    protected StatefulAggregateRoot _aggregateRoot;
    protected EventSourceId _eventSourceId;
    protected IImmutableList<EventType> _eventTypes;
    protected AggregateRootContext _aggregateRootContext;
    protected IUnitOfWork _unitOfWork;
    protected IServiceProvider _serviceProvider;

    void Establish()
    {
        _aggregateRoot = new StatefulAggregateRoot();
        _eventSourceId = EventSourceId.New();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _aggregateRootContext = new AggregateRootContext(
            EventSourceType.Default,
            _eventSourceId,
            _aggregateRoot.GetEventStreamType(),
            EventStreamId.Default,
            _eventSequence,
            _aggregateRoot,
            _unitOfWork,
            EventSequenceNumber.First,
            EventSequenceNumber.First);

        _eventTypes = new EventType[]
        {
            FirstEventType.EventTypeId,
            SecondEventType.EventTypeId
        }.ToImmutableList();

        _reducer.EventTypes.Returns(_eventTypes);

        _provider = new ReducerAggregateRootStateProvider<StateForAggregateRoot>(
            _aggregateRootContext,
            _reducer,
            _serviceProvider);
    }
}
