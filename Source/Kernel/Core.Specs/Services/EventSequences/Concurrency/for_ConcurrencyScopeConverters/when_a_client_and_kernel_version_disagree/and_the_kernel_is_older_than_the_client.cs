// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Services.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_a_client_and_kernel_version_disagree;

/// <summary>
/// <para>
/// The dangerous direction, and the reason the expectation is not carried as a distinguished sequence number.
/// A kernel that predates the dedicated field reads field 1 and ignores field 7 - so what it sees is the current
/// client's contract with the expectation stripped, which is exactly what this spec validates. Its conversion and
/// its validator are byte-identical to the ones here for a scope whose expectation is unset, so running today's
/// code against a stripped contract is the older kernel's behavior rather than an approximation of it.
/// </para>
/// <para>
/// The number the current client sends is <see cref="EventSequenceNumber.Unavailable"/>
/// (pinned on the client side by <c>for_ConcurrencyScopeConverters/when_converting_to_contract/and_the_scope_expects_no_matching_event</c>),
/// which such a kernel already declines to validate - so the mismatch degrades to the older, documented skip. Had
/// the expectation ridden in the sequence number instead, that kernel would have compared a real tail against a
/// number nothing can exceed and passed the check without running it, while the operator believed a guard was in
/// force.
/// </para>
/// </summary>
public class and_the_kernel_is_older_than_the_client : given.a_validator_reading_what_arrived_on_the_wire
{
    void Establish() => MatchingEventExistsAt(7UL);

    async Task Because() => await Validate(WhatAnOlderKernelReads(FirstAppendIntoANarrowedScope(declaresTheExpectation: true)));

    [Fact] void should_fall_back_to_the_older_skip() => _scope.IsIncomplete.ShouldBeTrue();
    [Fact] void should_report_the_append_as_unchecked() => _scope.ShouldBeValidated.ShouldBeFalse();
    [Fact] void should_not_invent_an_expectation_from_the_number() => _scope.ExpectsNoMatchingEvent.ShouldBeFalse();
    [Fact] void should_not_report_a_violation() => _result.HasValue.ShouldBeFalse();
    [Fact] void should_not_compare_a_real_tail_against_a_number_nothing_can_exceed() =>
        _eventSequenceStorage.DidNotReceive().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());

    /// <summary>
    /// Drop the field a kernel older than it never deserializes, leaving the wire exactly as such a kernel sees it.
    /// </summary>
    /// <param name="sent">The <see cref="Contracts.EventSequences.Concurrency.ConcurrencyScope"/> the client sent.</param>
    /// <returns>The scope an older kernel reads.</returns>
    static Contracts.EventSequences.Concurrency.ConcurrencyScope WhatAnOlderKernelReads(
        Contracts.EventSequences.Concurrency.ConcurrencyScope sent)
    {
        sent.ExpectsNoMatchingEvent = false;
        return sent;
    }
}
