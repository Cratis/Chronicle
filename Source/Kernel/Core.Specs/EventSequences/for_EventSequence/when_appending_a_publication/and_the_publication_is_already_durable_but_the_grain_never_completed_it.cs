// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_a_publication;

/// <summary>
/// Crash after the storage insert and before CompleteDurableAppend: the retry lands on a fresh grain activation that
/// knows nothing of the event. The receipt hit must finish the reconciliation, not only report success.
/// </summary>
public class and_the_publication_is_already_durable_but_the_grain_never_completed_it : given.an_event_sequence_with_publication_storage
{
    AppendResult _result;

    void Establish() => _publicationStorage.TryGetPublication(Arg.Any<EventPublication>())
        .Returns(_ => Task.FromResult<Option<EventPublicationReceipt>>(new EventPublicationReceipt(DurableSlot)));

    async Task Because() => _result = await Publish();

    [Fact] void should_report_success_with_the_durable_slot() => _result.SequenceNumber.ShouldEqual(DurableSlot);
    [Fact] void should_not_append_again() => _publicationStorage.DidNotReceive().AppendPublication(Arg.Any<EventPublication>(), Arg.Any<EventToAppendToStorage>());
    [Fact] void should_advance_the_sequence_number_past_the_durable_slot() => _stateStorage.State.SequenceNumber.ShouldEqual(DurableSlot.Next());
    [Fact] void should_record_the_tail_of_the_event_type() => _stateStorage.State.TailSequenceNumberPerEventType[_eventType.Id].ShouldEqual(DurableSlot);
    [Fact] void should_hand_the_event_to_the_observers() => _appendedEventsQueues.Received(1).Enqueue(Arg.Is<IEnumerable<AppendedEvent>>(_ => _.Single().Context.SequenceNumber == DurableSlot));
    [Fact] void should_update_the_constraint_index_with_the_durable_slot() => _constraintIndexSequenceNumbers.ShouldContainOnly(DurableSlot);
}
