// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_slot_retry_invalidates_the_decision : given.an_event_sequence_with_effect_receipts
{
    void Establish()
    {
        _collision = EffectReceiptWriteResult.DuplicateEventSequenceNumber;
        _eventSequenceStorage.GetTailSequenceNumber().Returns((EventSequenceNumber)43UL);
        _eventSequenceStorage.GetTailSequenceNumber(null, _eventSourceId, null, null, null).Returns((EventSequenceNumber)42UL, (EventSequenceNumber)43UL);
    }

    async Task Because() => _result = await AppendEffect(scopes: new(new Dictionary<EventSourceId, ConcurrencyScope>
    {
        [_eventSourceId] = new(42UL, true, null, null, null, null)
    }));

    [Fact] void should_refuse_the_stale_decision() => _result.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_report_the_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] async Task should_attempt_only_one_receipt_write() => await _receiptStorage.Received(1).AppendManyWithEffectReceipt(Arg.Any<IEnumerable<EventToAppendToStorage>>(), Arg.Any<EffectReceipt>());
    [Fact] async Task should_record_no_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldBeNull();
    [Fact] void should_append_no_events() => _durableStorage.Events.ShouldBeEmpty();
}
