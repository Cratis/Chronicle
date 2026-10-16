// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class and_the_effect_is_new : given.an_event_sequence_with_effect_receipts
{
    async Task Because() => _result = await AppendEffect();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_one_event() => _durableStorage.Events.Count.ShouldEqual(1);
    [Fact] void should_report_an_appended_outcome() => _result.EffectReceipt!.Outcome.ShouldEqual(EffectOutcome.Appended);
    [Fact] void should_record_the_response_verbatim() => _result.EffectReceipt!.Response.ShouldEqual("{\"answer\":42}");
    [Fact] async Task should_persist_the_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId))!.FirstSequenceNumber!.Value.ShouldEqual(0UL);
    [Fact] void should_not_use_an_ordinary_append() => _eventSequenceStorage.DidNotReceiveWithAnyArgs().AppendMany(default!);
}
