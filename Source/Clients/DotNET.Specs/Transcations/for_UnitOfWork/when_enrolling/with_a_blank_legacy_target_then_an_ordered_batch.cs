// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_blank_legacy_target_then_an_ordered_batch : given.a_unit_of_work
{
    LegacyEvent _legacyEvent;
    LaterLegacyEvent _laterLegacyEvent;
    Exception _error;
    Exception _laterLegacyError;

    void Establish()
    {
        _legacyEvent = new();
        _unitOfWork.AddEvent(EventSequenceId.Log, EventSourceId.Unspecified, _legacyEvent, Causation.Unknown());
    }

    void Because()
    {
        _error = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), new RejectedOrderedEvent())],
            []));
        _laterLegacyEvent = new();
        _laterLegacyError = Catch.Exception(() => _unitOfWork.AddEvent(
            EventSequenceId.Outbox,
            EventSourceId.New(),
            _laterLegacyEvent,
            Causation.Unknown()));
    }

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<ConcurrencyScopeLabelMustBeSpecified>();
    [Fact] void should_not_stage_the_ordered_event() => _unitOfWork.GetEvents().ShouldContainOnly(_legacyEvent, _laterLegacyEvent);
    [Fact] void should_leave_pure_legacy_sequence_behavior_active() => _laterLegacyError.ShouldBeNull();

    record LegacyEvent;
    record RejectedOrderedEvent;
    record LaterLegacyEvent;
}
