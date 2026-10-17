// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

public class and_scope_expects_no_matching_event_but_one_arrived_first_with_a_conflicting_event : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    EventType _conflictingEventType;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _conflictingEventType = new EventType("CustomerRegistered", 1);
        _scope = new ConcurrencyScope(EventSequenceNumber.BeforeFirst, true, null, null, new EventSourceType("Customer"), null);

        _eventSequenceStorage.GetTailSequenceNumber(
            _scope.EventTypes,
            _eventSourceId,
            _scope.EventSourceType,
            _scope.EventStreamId,
            _scope.EventStreamType).Returns(7UL);

        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true);
        cursor.Current.Returns([AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_conflictingEventType, 5UL)]);
        _eventSequenceStorage.GetFromSequenceNumber(
            EventSequenceNumber.First,
            _eventSourceId,
            _scope.EventSourceType,
            _scope.EventStreamType,
            _scope.EventStreamId,
            _scope.EventTypes).Returns(cursor);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_report_a_violation() => _result.HasValue.ShouldBeTrue();
    [Fact] void should_report_the_type_of_the_first_matching_event() => _result.AsT0.ConflictingEventTypeId.ShouldEqual(_conflictingEventType.Id);
    [Fact] void should_report_the_sequence_number_of_the_first_matching_event() => _result.AsT0.ConflictingSequenceNumber.ShouldEqual(new EventSequenceNumber(5UL));
}
