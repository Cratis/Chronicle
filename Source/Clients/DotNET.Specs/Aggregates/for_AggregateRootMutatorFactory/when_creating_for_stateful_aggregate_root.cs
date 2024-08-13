// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootMutatorFactory;

public class when_creating_for_stateful_aggregate_root : given.an_aggregate_root_mutator_factory
{
    AggregateRootContext _context;
    IAggregateRootMutator _result;
    StatefulAggregateRoot _aggregateRoot;

    void Establish()
    {
        _aggregateRoot = new StatefulAggregateRoot();
        _context = new AggregateRootContext(
        EventSourceId.New(),
        Substitute.For<IEventSequence>(),
        _aggregateRoot,
        Substitute.For<IUnitOfWork>());
    }

    async Task Because() => _result = await _factory.Create<StatefulAggregateRoot>(_context);

    [Fact] void should_return_a_stateful_mutator() => _result.ShouldBeOfExactType<StatefulAggregateRootMutator<StateForAggregateRoot>>();
    [Fact] void should_set_state_provider_on_mutator() => _aggregateRoot._state.ShouldNotBeNull();
}
