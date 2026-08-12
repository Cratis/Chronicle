// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_an_ordered_batch_then_a_throwing_legacy_scope : given.a_unit_of_work
{
    OrderedEvent _orderedEvent;
    Exception _error;

    void Establish()
    {
        _orderedEvent = new();
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), _orderedEvent)],
            []);
    }

    async Task Because()
    {
        _error = Catch.Exception(() => _unitOfWork.AddEvent(
            EventSequenceId.Log,
            EventSourceId.New(),
            new RejectedLegacyEvent(),
            Causation.Unknown(),
            concurrencyScope: new ConcurrencyScope(42UL, EventTypes: FailingEventTypes())));
        await _unitOfWork.Commit();
    }

    [Fact] void should_surface_the_enumeration_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_stage_the_legacy_event() => _unitOfWork.GetEvents().ShouldContainOnly(_orderedEvent);
    [Fact] void should_append_only_the_ordered_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_orderedEvent);

    static IEnumerable<EventType> FailingEventTypes()
    {
        yield return new EventType("legacy", 1);
        throw new InvalidOperationException("Event types could not be read");
    }

    record OrderedEvent;
    record RejectedLegacyEvent;
}
