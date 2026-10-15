// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_no_events_have_a_satisfied_scope : given.an_event_sequence_with_effect_receipts
{
    void Establish() => _eventSequenceStorage.GetTailSequenceNumber(null, _eventSourceId, null, null, null).Returns((EventSequenceNumber)42UL);
    async Task Because() => _result = await AppendEffect([], new(new Dictionary<EventSourceId, ConcurrencyScope>
    {
        [_eventSourceId] = new(42UL, true, null, null, null, null)
    }));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_check_the_scope() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_record_no_events() => _result.EffectReceipt!.Outcome.ShouldEqual(EffectOutcome.NoEvents);
    [Fact] async Task should_persist_the_no_events_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId))!.Outcome.ShouldEqual(EffectOutcome.NoEvents);
    [Fact] void should_append_nothing() => _durableStorage.Events.ShouldBeEmpty();
}
