// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_ReactorDefinitionConverters.when_converting_to_chronicle;

public class with_explicit_true : Specification
{
    ReactorDefinition _contract;
    Concepts.Observation.Reactors.ReactorDefinition _result;

    void Establish()
    {
        using var payload = new MemoryStream([0x0a, 0x01, 0x78, 0x12, 0x01, 0x79, 0x20, 0x00, 0x3a, 0x02, 0x08, 0x01]);
        _contract = Serializer.Deserialize<ReactorDefinition>(payload);
    }

    void Because() => _result = _contract.ToChronicle();

    [Fact] void should_decode_the_legacy_false_field() => _contract.IsReplayable.ShouldBeFalse();
    [Fact] void should_use_explicit_true_in_the_kernel() => _result.IsReplayable.ShouldBeTrue();
}
