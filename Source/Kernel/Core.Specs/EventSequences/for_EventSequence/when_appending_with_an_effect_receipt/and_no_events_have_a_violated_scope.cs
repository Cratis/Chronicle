// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_no_events_have_a_violated_scope : given.an_event_sequence_with_effect_receipts
{
    void Establish() => _eventSequenceStorage.GetTailSequenceNumber(null, _eventSourceId, null, null, null).Returns((EventSequenceNumber)43UL);
    async Task Because() => _result = await AppendEffect([], new(new Dictionary<EventSourceId, ConcurrencyScope>
    {
        [_eventSourceId] = new(42UL, true, null, null, null, null)
    }));

    [Fact] void should_refuse_the_decision() => _result.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_check_the_scope() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] async Task should_record_no_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldBeNull();
    [Fact] void should_not_call_receipt_storage() => _receiptStorage.DidNotReceiveWithAnyArgs().AppendManyWithEffectReceipt(default!, default!);
}
