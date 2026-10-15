// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_the_proposed_slot_is_already_durable : given.an_event_sequence_with_effect_receipts
{
    async Task Establish()
    {
        await AppendEffect();
        _stateStorage.State.SequenceNumber = EventSequenceNumber.First;
        _receipt = _receipt with { EffectKey = "another-effect" };
        _eventSequenceStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.First);
    }

    async Task Because() => _result = await AppendEffect();

    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_both_effects() => _durableStorage.Events.Count.ShouldEqual(2);
    [Fact] void should_renumber_the_event() => _result.SequenceNumbers.Single().Value.ShouldEqual(1UL);
    [Fact] void should_record_the_renumbered_range() => _result.EffectReceipt!.FirstSequenceNumber.ShouldEqual(1UL);
    [Fact] void should_advance_the_next_slot() => _stateStorage.State.SequenceNumber.Value.ShouldEqual(2UL);
}
