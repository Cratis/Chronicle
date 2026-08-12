// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_batch_then_add_event_for_another_sequence : given.a_unit_of_work
{
    FirstEvent _firstEvent;
    Exception _error;

    void Establish()
    {
        _firstEvent = new();
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(EventSourceId.New(), _firstEvent)], []);
    }

    async Task Because()
    {
        _error = Catch.Exception(() => _unitOfWork.AddEvent(
            EventSequenceId.Outbox,
            EventSourceId.New(),
            new RejectedEvent(),
            Causation.Unknown()));
        await _unitOfWork.Commit();
    }

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<UnitOfWorkCannotSpanEventSequences>();
    [Fact] void should_not_stage_the_rejected_event() => _unitOfWork.GetEvents().ShouldContainOnly(_firstEvent);
    [Fact] void should_append_only_the_first_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_firstEvent);
    [Fact] void should_not_resolve_the_wrong_sequence() => _eventStore.DidNotReceive().GetEventSequence(EventSequenceId.Outbox);

    record FirstEvent;
    record RejectedEvent;
}
