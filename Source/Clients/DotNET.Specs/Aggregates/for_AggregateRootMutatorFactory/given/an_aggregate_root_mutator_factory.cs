// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootMutatorFactory.given;

public class an_aggregate_root_mutator_factory : Specification
{
    protected AggregateRootMutatorFactory _factory;
    protected IEventStore _eventStore;
    protected IAggregateRootStateProviders _stateProviders;
    protected IAggregateRootEventHandlersFactory _eventHandlersFactory;
    protected IEventSerializer _eventSerializer;
    protected ICorrelationIdAccessor _correlationIdAccessor;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _stateProviders = Substitute.For<IAggregateRootStateProviders>();
        _eventHandlersFactory = Substitute.For<IAggregateRootEventHandlersFactory>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();

        _factory = new AggregateRootMutatorFactory(
            _eventStore,
            _stateProviders,
            _eventHandlersFactory,
            _eventSerializer,
            _correlationIdAccessor);
    }
}
