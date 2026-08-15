// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy.when_getting_a_scope;

/// <summary>
/// No event matches the narrowing yet, so the tail read answers <see cref="EventSequenceNumber.Unavailable"/> - which
/// also means "no expected sequence number was supplied at all" and is therefore skipped by the kernel. The strategy
/// says <see cref="EventSequenceNumber.BeforeFirst"/> instead, the expectation the first append into a scope actually
/// has: no event matching this narrowing may exist. The kernel checks that, so the first append into a scope is no
/// longer the one append in the system that goes unchecked.
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

    [Fact] void should_expect_the_position_before_the_first_event() => _result.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_expect_no_event_matching_the_narrowing() => _result.ExpectsNoMatchingEvent.ShouldBeTrue();
    [Fact] void should_not_produce_a_scope_the_kernel_skips() => _result.IsIncomplete.ShouldBeFalse();
    [Fact] void should_not_carry_the_unavailable_tail_it_read() => _result.SequenceNumber.ShouldNotEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_produce_the_scope_that_opts_out_of_checking() => _result.ShouldNotEqual(ConcurrencyScope.None);
    [Fact] void should_still_declare_the_narrowing_it_was_asked_for() => _result.EventSourceType.ShouldEqual(_eventSourceType);
}
