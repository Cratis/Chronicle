// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using ProtoBuf;

namespace Cratis.Chronicle.Occurrences.for_EffectReceiptConverters.when_round_tripping_the_wire;

public class and_the_receipt_has_an_appended_range : Specification
{
    Contracts.Occurrences.EffectReceiptDetails _result;
    EffectReceiptRecord _lookup;

    void Because()
    {
        var stored = new Storage.Occurrences.EffectReceipt("effect:identity", new(new("occurrence"), 7), EffectOutcome.Appended, "event-log", 0UL, 2UL, ["reason"], "{ \"answer\": [42, true] }", DateTimeOffset.Parse("2026-10-14T12:00:00+02:00"));
        _result = Serializer.DeepClone(EffectReceiptConverters.ToDetails(stored).ToContract());
        _lookup = EffectReceiptConverters.ToRecord(stored);
    }

    [Fact] void should_preserve_the_receipt_identity() => _result.Id.ShouldEqual("effect:identity");
    [Fact] void should_preserve_the_occurrence_identity() => _result.OccurrenceId.ShouldEqual("occurrence");
    [Fact] void should_preserve_the_epoch() => _result.Epoch.ShouldEqual(7L);
    [Fact] void should_preserve_the_outcome() => _result.Outcome.ShouldEqual(Contracts.Storage.Occurrences.EffectOutcome.Appended);
    [Fact] void should_preserve_the_sequence() => _result.EventSequenceId.ShouldEqual("event-log");
    [Fact] void should_preserve_the_zero_first_slot() => _result.FirstSequenceNumber.ShouldEqual(0UL);
    [Fact] void should_preserve_the_last_slot() => _result.LastSequenceNumber.ShouldEqual(2UL);
    [Fact] void should_preserve_the_reasons() => _result.Reasons.ShouldContainOnly("reason");
    [Fact] void should_preserve_the_verbatim_response() => _result.Response.ShouldEqual("{ \"answer\": [42, true] }");
    [Fact] void should_preserve_the_recording_instant() => ((DateTimeOffset)_result.RecordedAt).ShouldEqual(DateTimeOffset.Parse("2026-10-14T12:00:00+02:00"));
    [Fact] void should_agree_with_the_lookup() => _lookup.Response.ShouldEqual(_result.Response);
    [Fact] void should_agree_with_the_lookup_range() => _lookup.FirstSequenceNumber.ShouldEqual(_result.FirstSequenceNumber);
}
