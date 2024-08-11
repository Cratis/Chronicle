// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootMutation.given;

public class an_aggregate_mutation : Specification
{
    protected AggregateRootMutation _mutation;
    protected IAggregateRootMutator _mutator;
    protected IEventSequence _eventSequence;
    protected ICausationManager _causationManager;
    protected EventSourceId _eventSourceId;
    protected EventSequenceId _eventSequenceId;
    protected IAggregateRootContext _aggregateRootContext;
    protected IAggregateRoot _aggregateRoot;
    protected IUnitOfWork _unitOfWork;

    void Establish()
    {
        _mutator = Substitute.For<IAggregateRootMutator>();
        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequenceId = new EventSequenceId(Guid.NewGuid().ToString());
        _eventSequence.Id.Returns(_eventSequenceId);
        _causationManager = Substitute.For<ICausationManager>();
        _eventSourceId = EventSourceId.New();

        _unitOfWork = Substitute.For<IUnitOfWork>();

        _aggregateRoot = new StatefulAggregateRoot();
        _aggregateRootContext = Substitute.For<IAggregateRootContext>();
        _aggregateRootContext.EventSourceId.Returns(_eventSourceId);
        _aggregateRootContext.AggregateRoot.Returns(_aggregateRoot);
        _aggregateRootContext.UnitOfWOrk.Returns(_unitOfWork);

        _mutation = new AggregateRootMutation(
            _aggregateRootContext,
            _mutator,
            _eventSequence);
    }
}
