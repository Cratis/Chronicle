// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

public class and_concurrency_violation_detected_with_a_conflicting_event : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    EventType _conflictingEventType;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _conflictingEventType = new EventType("BookBorrowed", 1);
        _scope = new ConcurrencyScope(42, true, null, null, new EventSourceType("Book"), [_conflictingEventType]);

        _eventSequenceStorage.GetTailSequenceNumber(
            _scope.EventTypes,
            _eventSourceId,
            _scope.EventSourceType,
            _scope.EventStreamId,
            _scope.EventStreamType).Returns(44UL);

        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true);
        cursor.Current.Returns([AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_conflictingEventType, 43UL)]);
        _eventSequenceStorage.GetFromSequenceNumber(
            43UL,
            _eventSourceId,
            _scope.EventSourceType,
            _scope.EventStreamType,
            _scope.EventStreamId,
            _scope.EventTypes).Returns(cursor);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_return_violation() => _result.HasValue.ShouldBeTrue();
    [Fact] void should_still_report_the_tail_as_the_actual_sequence_number() => _result.AsT0.ActualSequenceNumber.Value.ShouldEqual(44UL);
    [Fact] void should_report_the_type_of_the_first_event_after_the_expected_sequence_number() => _result.AsT0.ConflictingEventTypeId.ShouldEqual(_conflictingEventType.Id);
    [Fact] void should_report_the_sequence_number_of_the_first_event_after_the_expected_sequence_number() => _result.AsT0.ConflictingSequenceNumber.ShouldEqual(new EventSequenceNumber(43UL));
}
