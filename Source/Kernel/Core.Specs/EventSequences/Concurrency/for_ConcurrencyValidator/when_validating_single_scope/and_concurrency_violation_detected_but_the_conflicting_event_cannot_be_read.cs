// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

public class and_concurrency_violation_detected_but_the_conflicting_event_cannot_be_read : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scope = new ConcurrencyScope(42, true, null, null, null, null);

        _eventSequenceStorage.GetTailSequenceNumber(
            _scope.EventTypes,
            _eventSourceId,
            _scope.EventSourceType,
            _scope.EventStreamId,
            _scope.EventStreamType).Returns(44UL);

        _eventSequenceStorage.GetFromSequenceNumber(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<IEnumerable<EventType>?>()).ThrowsAsync(new InvalidOperationException("Storage is unavailable"));
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_still_report_the_violation() => _result.HasValue.ShouldBeTrue();
    [Fact] void should_report_the_actual_sequence_number() => _result.AsT0.ActualSequenceNumber.Value.ShouldEqual(44UL);
    [Fact] void should_not_report_a_conflicting_event_type() => _result.AsT0.ConflictingEventTypeId.ShouldBeNull();
    [Fact] void should_not_report_a_conflicting_sequence_number() => _result.AsT0.ConflictingSequenceNumber.ShouldBeNull();
}
