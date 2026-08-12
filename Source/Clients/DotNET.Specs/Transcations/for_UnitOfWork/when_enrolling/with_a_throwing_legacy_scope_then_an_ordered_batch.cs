// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_throwing_legacy_scope_then_an_ordered_batch : given.a_unit_of_work
{
    LegacyEvent _legacyEvent;
    LaterLegacyEvent _laterLegacyEvent;
    Exception _error;
    Exception _laterLegacyError;

    void Establish()
    {
        _legacyEvent = new();
        _unitOfWork.AddEvent(
            EventSequenceId.Log,
            EventSourceId.New(),
            _legacyEvent,
            Causation.Unknown(),
            concurrencyScope: new ConcurrencyScope(42UL, EventTypes: FailingEventTypes()));
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

    [Fact] void should_surface_the_enumeration_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_stage_the_ordered_event() => _unitOfWork.GetEvents().ShouldContainOnly(_legacyEvent, _laterLegacyEvent);
    [Fact] void should_leave_pure_legacy_sequence_behavior_active() => _laterLegacyError.ShouldBeNull();

    static IEnumerable<EventType> FailingEventTypes()
    {
        yield return new EventType("legacy", 1);
        throw new InvalidOperationException("Event types could not be read");
    }

    record LegacyEvent;
    record RejectedOrderedEvent;
    record LaterLegacyEvent;
}
