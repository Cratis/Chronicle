// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class with_equal_predicates_and_a_later_earlier_boundary : given.a_definition_backed_batch
{
    void Establish()
    {
        _concurrencyScopeStrategies.GetFor(_eventSequence).Returns(_concurrencyScopeStrategy);
        _concurrencyScopeStrategy.GetScope(_sourceId, Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>(), Arg.Any<IEnumerable<EventType>?>())
            .Returns(new ConcurrencyScope(50, _sourceId), new ConcurrencyScope(20, _sourceId));
    }

    Task Because() => Append([new(_sourceId, "first") { EventStreamId = "first" }, new(_sourceId, "second") { EventStreamId = "second" }]);

    [Fact] void should_not_require_conjunction_support_for_equal_predicates() => _exception.ShouldBeNull();
    [Fact] void should_keep_one_scope() => _request.ConcurrencyScopes.Count().ShouldEqual(1);
    [Fact] void should_keep_the_earliest_expectation() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(20UL);
}
