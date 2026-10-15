// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_the_collision_cannot_be_settled : given.an_event_sequence_with_effect_receipts
{
    void Establish() => _collision = EffectReceiptWriteResult.TransactionConflicted;
    async Task Because() => _result = await AppendEffect();

    [Fact] void should_fail_retryably() => _result.Errors.ShouldContain(AppendError.EffectReceiptUnsettled);
    [Fact] void should_append_nothing() => _durableStorage.Events.ShouldBeEmpty();
    [Fact] async Task should_write_no_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldBeNull();
}
