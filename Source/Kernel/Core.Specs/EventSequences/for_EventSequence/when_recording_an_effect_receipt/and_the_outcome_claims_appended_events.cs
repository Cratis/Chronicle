// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_recording_an_effect_receipt;

public class and_the_outcome_claims_appended_events : given.an_event_sequence_with_effect_receipts
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.RecordEffectReceipt(_receipt, EffectOutcome.Appended, []));

    [Fact] void should_refuse_the_invalid_outcome() => _error.ShouldBeOfExactType<InvalidEffectReceipt>();
    [Fact] async Task should_record_no_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldBeNull();
    [Fact] async Task should_not_attempt_a_write() => await _receiptStorage.DidNotReceiveWithAnyArgs().AppendManyWithEffectReceipt(default!, default!);
}
