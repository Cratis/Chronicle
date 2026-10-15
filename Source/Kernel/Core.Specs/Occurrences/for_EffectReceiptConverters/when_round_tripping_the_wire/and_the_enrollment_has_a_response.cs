// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Occurrences.for_EffectReceiptConverters.when_round_tripping_the_wire;

public class and_the_enrollment_has_a_response : Specification
{
    EffectReceipt _result;

    void Because() => _result = Serializer.DeepClone(new Contracts.Occurrences.EffectReceipt
    {
        OccurrenceId = "occurrence",
        EffectKey = "command",
        Epoch = 4,
        Response = "{ \"answer\": 42 }"
    }).ToApi();

    [Fact] void should_preserve_the_occurrence() => _result.OccurrenceId.ShouldEqual("occurrence");
    [Fact] void should_preserve_the_effect_key() => _result.EffectKey.ShouldEqual("command");
    [Fact] void should_preserve_the_epoch() => _result.Epoch.ShouldEqual(4L);
    [Fact] void should_preserve_the_verbatim_response() => _result.Response.ShouldEqual("{ \"answer\": 42 }");
}
