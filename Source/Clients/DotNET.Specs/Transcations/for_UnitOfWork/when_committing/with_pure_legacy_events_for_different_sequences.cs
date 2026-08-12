// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_pure_legacy_events_for_different_sequences : given.a_unit_of_work
{
    IEventSequence _secondEventSequence;
    FirstEvent _firstEvent;
    SecondEvent _secondEvent;

    void Establish()
    {
        _secondEventSequence = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(EventSequenceId.Outbox).Returns(_secondEventSequence);
        _firstEvent = new();
        _secondEvent = new();
        _unitOfWork.AddEvent(EventSequenceId.Log, EventSourceId.New(), _firstEvent, Causation.Unknown());
        _unitOfWork.AddEvent(EventSequenceId.Outbox, EventSourceId.New(), _secondEvent, Causation.Unknown());
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_append_both_events_to_the_first_sequence() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_firstEvent, _secondEvent);
    [Fact] void should_preserve_the_legacy_second_sequence_lookup() => _eventStore.Received(1).GetEventSequence(EventSequenceId.Outbox);
    [Fact] void should_not_append_to_the_second_sequence() => _secondEventSequence.DidNotReceiveWithAnyArgs().AppendMany(default!, default, default, default);

    record FirstEvent;
    record SecondEvent;
}
