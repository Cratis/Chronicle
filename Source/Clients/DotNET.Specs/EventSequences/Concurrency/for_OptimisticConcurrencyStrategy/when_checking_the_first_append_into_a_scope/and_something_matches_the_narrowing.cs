// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy.when_checking_the_first_append_into_a_scope;

/// <summary>
/// Opting in changes one thing only. An append extending a scope that already has events resolves the tail it
/// always resolved, and is validated the way it always was - the option decides what "no tail" means, not how a
/// tail is used.
/// </summary>
public class and_something_matches_the_narrowing : given.an_optimistic_concurrency_strategy_that_checks_the_first_append
{
    ConcurrencyScope _result;

    async Task Because() => _result = await _strategy.GetScope(_eventSourceId, eventSourceType: new EventSourceType("Customer"));

    [Fact] void should_carry_the_tail_it_read() => _result.SequenceNumber.ShouldEqual(_tail);
    [Fact] void should_not_expect_that_no_matching_event_exists() => _result.ExpectsNoMatchingEvent.ShouldBeFalse();
}
