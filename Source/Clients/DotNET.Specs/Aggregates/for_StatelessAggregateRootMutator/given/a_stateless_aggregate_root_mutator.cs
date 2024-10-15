// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates.for_StatelessAggregateRootMutator.given;

public class a_stateless_aggregate_root_mutator : Specification
{
    protected StatelessAggregateRoot _aggregateRoot;

    protected IEventStore _eventStore;
    protected EventSourceId _eventSourceId;
    protected AggregateRootContext _aggregateRootContext;
    protected IAggregateRootMutator _mutator;
    protected IEventSerializer _eventSerializer;
    protected IAggregateRootEventHandlers _eventHandlers;
    protected IEventSequence _eventSequence;
    protected IUnitOfWork _unitOfWork;
    protected ICorrelationIdAccessor _correlationIdAccessor;

    void Establish()
    {
        _aggregateRoot = new StatelessAggregateRoot();
        _eventStore = Substitute.For<IEventStore>();
        _eventSourceId = EventSourceId.New();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _eventSequence = Substitute.For<IEventSequence>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _aggregateRootContext = new AggregateRootContext(
            EventSource.Default,
            _eventSourceId,
            _aggregateRoot.GetEventStreamType(),
            EventStreamId.Default,
            _eventSequence,
            _aggregateRoot,
            _unitOfWork,
            EventSequenceNumber.First);

        _eventSerializer = Substitute.For<IEventSerializer>();
        _eventHandlers = Substitute.For<IAggregateRootEventHandlers>();
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();

        _mutator = new StatelessAggregateRootMutator(
            _aggregateRootContext,
            _eventStore,
            _eventSerializer,
            _eventHandlers,
            _correlationIdAccessor);
    }
}
