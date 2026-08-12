// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_scope_event_types_that_fail_during_enumeration : given.a_unit_of_work
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEvents(
        EventSequenceId.Log,
        [new(EventSourceId.New(), new RejectedEvent())],
        [new(EventSourceId.New(), new ConcurrencyScope(42UL, EventTypes: FailingEventTypes()))]));

    [Fact] void should_surface_the_enumeration_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_stage_the_event() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_bind_the_event_sequence() => _eventStore.DidNotReceive().GetEventSequence(EventSequenceId.Log);

    static IEnumerable<EventType> FailingEventTypes()
    {
        yield return new EventType("event", 1);
        throw new InvalidOperationException("Event types could not be read");
    }

    record RejectedEvent;
}
