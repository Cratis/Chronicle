// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_a_strategy_that_checks_nothing : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        _defaultConcurrencyScope = ConcurrencyScope.None;
        _concurrencyScopeStrategy.GetScope(Arg.Any<EventSourceId>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<IEnumerable<EventType>?>()).Returns(_defaultConcurrencyScope);
        _concurrencyScopeStrategies.GetFor(Arg.Any<IEventSequence>()).Returns(_concurrencyScopeStrategy);
        Guard();
        Enroll();
    }
    Task Because() => Commit();
    [Fact] void should_send_the_guard_instead_of_none() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}
