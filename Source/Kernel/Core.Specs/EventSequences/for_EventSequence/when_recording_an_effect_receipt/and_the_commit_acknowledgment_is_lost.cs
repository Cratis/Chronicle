// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_recording_an_effect_receipt;

public class and_the_commit_acknowledgment_is_lost : given.an_event_sequence_with_effect_receipts
{
    RecordEffectReceiptResult _recorded;

    void Establish() => _commitThenFail = true;
    async Task Because() => _recorded = await _eventSequence.RecordEffectReceipt(_receipt, EffectOutcome.NoEvents, []);

    [Fact] void should_settle_from_the_durable_receipt() => _recorded.Status.ShouldEqual(EffectReceiptStatus.AlreadyRecorded);
    [Fact] void should_return_the_committed_response() => _recorded.Receipt.Response.ShouldEqual("{\"answer\":42}");
    [Fact] async Task should_keep_the_durable_receipt() => (await _receiptStorage.GetEffectReceipt(ReceiptId)).ShouldNotBeNull();
    [Fact] void should_append_no_events() => _durableStorage.Events.ShouldBeEmpty();
}
