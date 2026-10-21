// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_recording_an_effect_receipt;

public class and_another_writer_already_recorded_it : given.an_event_sequence_with_effect_receipts
{
    RecordEffectReceiptResult _recorded;

    async Task Establish()
    {
        await _eventSequence.RecordEffectReceipt(_receipt, EffectOutcome.Refused, ["business refusal"]);
        _receipt = _receipt with { Epoch = 2, Response = "invalid JSON" };
    }

    async Task Because() => _recorded = await _eventSequence.RecordEffectReceipt(_receipt, EffectOutcome.NoEvents, []);

    [Fact] void should_report_already_recorded() => _recorded.Status.ShouldEqual(EffectReceiptStatus.AlreadyRecorded);
    [Fact] void should_keep_the_first_outcome() => _recorded.Receipt.Outcome.ShouldEqual(EffectOutcome.Refused);
    [Fact] void should_keep_the_first_response() => _recorded.Receipt.Response.ShouldEqual("{\"answer\":42}");
    [Fact] void should_keep_the_first_reasons() => _recorded.Receipt.Reasons.ShouldContainOnly("business refusal");
    [Fact] void should_append_no_events() => _durableStorage.Events.ShouldBeEmpty();
}
