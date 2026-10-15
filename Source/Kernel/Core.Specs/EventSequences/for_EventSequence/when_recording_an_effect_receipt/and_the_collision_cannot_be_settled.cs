// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_recording_an_effect_receipt;

public class and_the_collision_cannot_be_settled : given.an_event_sequence_with_effect_receipts
{
    Exception _error;

    void Establish() => _collision = EffectReceiptWriteResult.TransactionConflicted;
    async Task Because() => _error = await Catch.Exception(() => _eventSequence.RecordEffectReceipt(_receipt, EffectOutcome.NoEvents, []));

    [Fact] void should_fail_retryably() => _error.ShouldBeOfExactType<EffectReceiptUnsettled>();
    [Fact] void should_preserve_the_retry_classification() => _error.Message.ShouldEqual("EffectReceiptUnsettled");
    [Fact] async Task should_record_no_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldBeNull();
    [Fact] void should_append_no_events() => _durableStorage.Events.ShouldBeEmpty();
}
