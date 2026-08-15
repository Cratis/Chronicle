// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy.when_getting_a_scope;

/// <summary>
/// <para>
/// DO NOT CHANGE THIS SPEC TO EXPECT A CHECK. It is what keeps checking the first append a minor release rather
/// than a major one. Out of the box, nothing configured, the first append into a narrowed scope resolves the same
/// unchecked scope it has resolved in every released version - the tail read answers
/// <see cref="EventSequenceNumber.Unavailable"/> and that is what the scope carries, so the kernel skips the check
/// and the append goes through. Every consumer upgrading into this release keeps that behavior, and no append that
/// succeeds today starts being rejected.
/// </para>
/// <para>
/// The checked behavior is opt-in and lives in
/// <c>when_checking_the_first_append_into_a_scope/and_nothing_matches_the_narrowing</c>. If this spec ever has to
/// change, the release containing that change is a major one, and the default is being flipped deliberately - see
/// <see cref="ConcurrencyOptions.CheckFirstAppendIntoAScopeByDefault"/>.
/// </para>
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
    [Fact] void should_produce_the_scope_the_kernel_skips() => _result.IsIncomplete.ShouldBeTrue();
    [Fact] void should_not_expect_that_no_matching_event_exists() => _result.ExpectsNoMatchingEvent.ShouldBeFalse();
    [Fact] void should_not_produce_the_scope_that_opts_out_of_checking() => _result.ShouldNotEqual(ConcurrencyScope.None);
    [Fact] void should_still_declare_the_narrowing_it_was_asked_for() => _result.EventSourceType.ShouldEqual(_eventSourceType);
}
