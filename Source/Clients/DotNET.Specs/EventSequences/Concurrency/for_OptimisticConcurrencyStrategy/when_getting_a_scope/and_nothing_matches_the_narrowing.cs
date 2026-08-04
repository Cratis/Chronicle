// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy.when_getting_a_scope;

/// <summary>
/// No event matches the narrowing yet, so the tail read answers <see cref="EventSequenceNumber.Unavailable"/> and
/// the strategy has no expected sequence number to hand back. The scope it produces is therefore the incomplete
/// one the kernel declines to validate, which means the first append into any scope is unchecked. Both halves of
/// that were pinned separately - the validator's skip on a hand-built scope, and the strategy's narrowing on a
/// tail that exists - and nothing pinned that the shipped default strategy is what produces the skipped scope.
/// </summary>
public class and_nothing_matches_the_narrowing : given.an_optimistic_concurrency_strategy
{
    static readonly EventSourceType _eventSourceType = new("Customer");

    ConcurrencyScope _result;

    void Establish() =>
        _eventSequence.GetTailSequenceNumber(
                Arg.Any<EventSourceId?>(),
                Arg.Any<EventSourceType?>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<EventStreamId?>(),
                Arg.Any<IEnumerable<EventType>?>())
            .Returns(EventSequenceNumber.Unavailable);

    async Task Because() => _result = await _strategy.GetScope(_eventSourceId, eventSourceType: _eventSourceType);

    [Fact] void should_carry_the_unavailable_tail_it_read() => _result.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_produce_an_incomplete_scope() => _result.IsIncomplete.ShouldBeTrue();
    [Fact] void should_not_produce_the_scope_that_opts_out_of_checking() => _result.ShouldNotEqual(ConcurrencyScope.None);
    [Fact] void should_still_declare_the_narrowing_it_was_asked_for() => _result.EventSourceType.ShouldEqual(_eventSourceType);
}
