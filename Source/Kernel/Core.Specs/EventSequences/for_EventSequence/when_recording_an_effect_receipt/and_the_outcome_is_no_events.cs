// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_recording_an_effect_receipt;

public class and_the_outcome_is_no_events : given.an_event_sequence_with_effect_receipts
{
    RecordEffectReceiptResult _recorded;

    async Task Because() => _recorded = await _eventSequence.RecordEffectReceipt(_receipt, EffectOutcome.NoEvents, []);

    [Fact] void should_report_recorded() => _recorded.Status.ShouldEqual(EffectReceiptStatus.Recorded);
    [Fact] void should_record_no_events_outcome() => _recorded.Receipt.Outcome.ShouldEqual(EffectOutcome.NoEvents);
    [Fact] void should_preserve_the_response() => _recorded.Receipt.Response.ShouldEqual("{\"answer\":42}");
    [Fact] void should_have_no_event_range() => _recorded.Receipt.FirstSequenceNumber.ShouldBeNull();
    [Fact] async Task should_make_the_receipt_durable() => (await _receiptStorage.GetEffectReceipt(ReceiptId))!.Outcome.ShouldEqual(EffectOutcome.NoEvents);
    [Fact] void should_append_no_events() => _durableStorage.Events.ShouldBeEmpty();
}
