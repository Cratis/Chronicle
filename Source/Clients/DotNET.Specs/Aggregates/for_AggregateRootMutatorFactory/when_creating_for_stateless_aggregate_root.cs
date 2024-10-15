// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootMutatorFactory;

public class when_creating_for_stateless_aggregate_root : given.an_aggregate_root_mutator_factory
{
    AggregateRootContext _context;
    IAggregateRootMutator _result;

    void Establish() => _context = new AggregateRootContext(
        EventSource.Default,
        EventSourceId.New(),
        EventStreamType.All,
        EventStreamId.Default,
        Substitute.For<IEventSequence>(),
        new StatelessAggregateRoot(),
        Substitute.For<IUnitOfWork>(),
        EventSequenceNumber.First);

    async Task Because() => _result = await _factory.Create<StatelessAggregateRoot>(_context);

    [Fact] void should_return_a_stateful_mutator() => _result.ShouldBeOfExactType<StatelessAggregateRootMutator>();
}
