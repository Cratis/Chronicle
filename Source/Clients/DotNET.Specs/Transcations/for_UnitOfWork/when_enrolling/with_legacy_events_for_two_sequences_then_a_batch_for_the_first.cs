// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_legacy_events_for_two_sequences_then_a_batch_for_the_first : given.a_unit_of_work
{
    IEventSequence _secondEventSequence;
    FirstLegacyEvent _firstLegacyEvent;
    SecondLegacyEvent _secondLegacyEvent;
    Exception _error;

    void Establish()
    {
        _secondEventSequence = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_secondEventSequence);
        _firstLegacyEvent = new();
        _secondLegacyEvent = new();
        _unitOfWork.AddEvent(EventSequenceId.Log, EventSourceId.New(), _firstLegacyEvent, Causation.Unknown());
        _unitOfWork.AddEvent(EventSequenceId.Outbox, EventSourceId.New(), _secondLegacyEvent, Causation.Unknown());
    }

    async Task Because()
    {
        _error = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), new RejectedOrderedEvent())],
            []));
        await _unitOfWork.Commit();
    }

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<UnitOfWorkCannotSpanEventSequences>();
    [Fact] void should_not_stage_the_ordered_event() => _unitOfWork.GetEvents().ShouldContainOnly(_firstLegacyEvent, _secondLegacyEvent);
    [Fact] void should_append_only_the_legacy_events_to_the_first_sequence() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_firstLegacyEvent, _secondLegacyEvent);
    [Fact] void should_not_resolve_the_second_sequence_again() => _eventStore.Received(1).GetEventSequence(EventSequenceId.Outbox);
    [Fact] void should_not_append_to_the_second_sequence() => _secondEventSequence.DidNotReceiveWithAnyArgs().AppendMany(default!, default, default, default);

    record FirstLegacyEvent;
    record SecondLegacyEvent;
    record RejectedOrderedEvent;
}
