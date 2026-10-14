// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_definition_routed_events_before_a_contributed_read : given.an_enforcing_unit
{
    void Establish()
    {
        var sources = Substitute.For<IEventSources>();
        sources.GetFor(typeof(object)).Returns(new EventSourceDefinition(typeof(object), "account", "", ConcurrencyDimensions.None, [new("Transactions", "", ConcurrencyDimensions.None)]));
        _eventStore.EventSources.Returns(sources);
        _unitOfWork.AddEvents(EventSequenceId.Log, [new("source", new object()) { EventSource = typeof(object), EventStream = "Transactions", EventStreamId = "stream-id" }], []);
        _contribution = _contribution with { Scope = new ConcurrencyScope(100, "source", "Transactions", "stream-id", "account", [_changed]) };
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.EnrolContributedRead(_contribution));

    [Fact] void should_refuse_the_stale_contributed_read() => _error.ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
}
