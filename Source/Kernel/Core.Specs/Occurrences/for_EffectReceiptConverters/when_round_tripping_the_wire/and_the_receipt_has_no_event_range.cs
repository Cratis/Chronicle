// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using ProtoBuf;

namespace Cratis.Chronicle.Occurrences.for_EffectReceiptConverters.when_round_tripping_the_wire;

public class and_the_receipt_has_no_event_range : Specification
{
    Contracts.Occurrences.EffectReceiptDetails _result;

    void Because()
    {
        var stored = new Storage.Occurrences.EffectReceipt("effect:identity", new(new("occurrence"), 7), EffectOutcome.NoEvents, null, null, null, [], null, DateTimeOffset.Parse("2026-10-14T12:00:00Z"));
        _result = Serializer.DeepClone(EffectReceiptConverters.ToDetails(stored).ToContract());
    }

    [Fact] void should_preserve_the_outcome() => _result.Outcome.ShouldEqual(Contracts.Storage.Occurrences.EffectOutcome.NoEvents);
    [Fact] void should_preserve_the_absent_sequence() => _result.EventSequenceId.ShouldBeNull();
    [Fact] void should_preserve_the_absent_first_slot() => _result.FirstSequenceNumber.ShouldBeNull();
    [Fact] void should_preserve_the_absent_last_slot() => _result.LastSequenceNumber.ShouldBeNull();
    [Fact] void should_preserve_the_absent_response() => _result.Response.ShouldBeNull();
}
