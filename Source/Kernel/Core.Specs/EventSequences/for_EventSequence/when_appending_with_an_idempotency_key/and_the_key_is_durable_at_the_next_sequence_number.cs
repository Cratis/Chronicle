// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_key_is_durable_at_the_next_sequence_number : given.an_event_sequence_with_idempotency_keys
{
    async Task Establish()
    {
        Durable();
        _stateStorage.State.SequenceNumber = EventSequenceNumber.First;
        await SeedSlot(EventSequenceNumber.First);
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_advance_past_the_receipt() => _stateStorage.State.SequenceNumber.ShouldEqual(EventSequenceNumber.First.Next());
    [Fact] void should_adopt_the_type_tail() => _stateStorage.State.TailSequenceNumberPerEventType[_eventType.Id].ShouldEqual(EventSequenceNumber.First);
    [Fact] async Task should_spill_the_unobserved_gap() => await _appendedEventsQueues.Received(1).SpillToCatchup();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
