// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootFactory.given;

public class an_aggregate_root_factory : Specification
{
    protected AggregateRootFactory _factory;
    protected IEventStore _eventStore;
    protected IAggregateRootMutatorFactory _mutatorFactory;
    protected IUnitOfWorkManager _unitOfWorkManager;
    protected IServiceProvider _serviceProvider;
    protected IAggregateRootMutator _mutator;
    protected IEventSequence _eventSequence;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventSequence = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);

        _mutatorFactory = Substitute.For<IAggregateRootMutatorFactory>();
        _unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _mutator = Substitute.For<IAggregateRootMutator>();
        _mutatorFactory.Create<StatelessAggregateRoot>(Arg.Any<AggregateRootContext>()).Returns(_mutator);

        _factory = new AggregateRootFactory(
            _eventStore,
            _mutatorFactory,
            _unitOfWorkManager,
            _serviceProvider);
    }
}
