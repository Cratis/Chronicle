// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

/// <summary>
/// A scope that narrows the append but says nothing about what it expects is neither NotSet nor None, so it
/// reaches the validator and is then skipped for want of a sequence number to compare against. The append
/// proceeds unchecked, which looks from the outside exactly like never having asked for a check.
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
    [Fact] void should_not_report_a_violation() => _result.HasValue.ShouldBeFalse();
    [Fact] void should_not_read_a_tail_to_compare_against() =>
        _eventSequenceStorage.DidNotReceive().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
}
