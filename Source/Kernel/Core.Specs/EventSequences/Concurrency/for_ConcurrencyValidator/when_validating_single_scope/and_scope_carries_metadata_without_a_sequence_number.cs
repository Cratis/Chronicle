// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

/// <summary>
/// <para>
/// This spec used to describe what the shipped default strategy produced for the first append into any narrowed
/// scope. It no longer does: <c>OptimisticConcurrencyStrategy</c> answers
/// <see cref="EventSequenceNumber.BeforeFirst"/> for an empty narrowing, and the kernel checks that. What is left
/// here is the case the state was always documented as - a caller that built a scope by hand and never resolved an
/// expected sequence number, where <c>None</c> (append without a check) or <c>NotSet</c> (let the strategy decide)
/// was what it wanted.
/// </para>
/// <para>
/// The skip is deliberately kept for that case: <see cref="EventSequenceNumber.Unavailable"/> is the number
/// <c>ConcurrencyScope.None</c> itself carries, so reading it as "expect nothing to exist" would turn every
/// opted-out append into a checked one. The expectation pinned here is therefore that
/// <see cref="EventSequenceNumber.Unavailable"/> and <see cref="EventSequenceNumber.BeforeFirst"/> stay two
/// different answers, and only the second one is a check.
/// </para>
/// </summary>
public class and_scope_carries_metadata_without_a_sequence_number : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scope = new ConcurrencyScope(EventSequenceNumber.Unavailable, false, null, null, new EventSourceType("Thing"), null);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_be_recognized_as_incomplete() => _scope.IsIncomplete.ShouldBeTrue();
    [Fact] void should_not_be_read_as_expecting_no_matching_event() => _scope.ExpectsNoMatchingEvent.ShouldBeFalse();
    [Fact] void should_still_skip_the_check_rather_than_reject_the_append() => _result.HasValue.ShouldBeFalse();
    [Fact] void should_not_read_a_tail_to_compare_against() =>
        _eventSequenceStorage.DidNotReceive().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
}
