// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_key_is_durable_beyond_the_next_sequence_number : given.an_event_sequence_with_idempotency_keys
{
    readonly EventType _other = new("other", 1);
    async Task Establish()
    {
        Durable(number: 2UL);
        _stateStorage.State.SequenceNumber = EventSequenceNumber.First;
        await SeedSlot(0UL, _other);
        await SeedSlot(1UL, _other);
        await SeedSlot(2UL);
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_advance_to_the_durable_tail() => _stateStorage.State.SequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_keep_the_gap_watermark() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_adopt_other_event_types_in_the_gap() => _stateStorage.State.TailSequenceNumberPerEventType[_other.Id].ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_account_for_the_original_type() => _stateStorage.State.TailSequenceNumberPerEventType[_eventType.Id].ShouldEqual((EventSequenceNumber)2UL);
    [Fact] async Task should_not_enqueue() => await _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
}
