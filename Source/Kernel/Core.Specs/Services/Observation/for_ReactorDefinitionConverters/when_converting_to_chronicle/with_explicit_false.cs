// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_ReactorDefinitionConverters.when_converting_to_chronicle;

public class with_explicit_false : Specification
{
    ReactorDefinition _contract;
    Concepts.Observation.Reactors.ReactorDefinition _result;

    void Establish()
    {
        // An ordinary proto3 client omits the legacy false field and writes an empty BoolValue at field 7.
        using var payload = new MemoryStream([0x0a, 0x01, 0x78, 0x12, 0x01, 0x79, 0x3a, 0x00]);
        _contract = Serializer.Deserialize<ReactorDefinition>(payload);
    }

    void Because() => _result = Serializer.DeepClone(_contract).ToChronicle();

    [Fact] void should_preserve_the_legacy_absent_field_default() => _contract.IsReplayable.ShouldBeTrue();
    [Fact] void should_use_explicit_false_in_the_kernel() => _result.IsReplayable.ShouldBeFalse();
}
