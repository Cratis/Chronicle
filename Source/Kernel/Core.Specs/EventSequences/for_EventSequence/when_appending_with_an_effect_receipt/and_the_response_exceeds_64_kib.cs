// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_the_response_exceeds_64_kib : given.an_event_sequence_with_effect_receipts
{
    void Establish() => _receipt = _receipt with { Response = $"\"{new string('x', 64 * 1024)}\"" };
    async Task Because() => _result = await AppendEffect();

    [Fact] void should_refuse_the_append() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_append_nothing() => _durableStorage.Events.ShouldBeEmpty();
    [Fact] async Task should_not_record_a_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldBeNull();
}
