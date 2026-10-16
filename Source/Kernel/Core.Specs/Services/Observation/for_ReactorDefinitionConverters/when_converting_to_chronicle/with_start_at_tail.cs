// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_ReactorDefinitionConverters.when_converting_to_chronicle;

public class with_start_at_tail : Specification
{
    ReactorDefinition _contract;
    Concepts.Observation.Reactors.ReactorDefinition _result;

    void Establish()
    {
        // Field 9 (varint) set to true, as a client that knows about it sends it.
        using var payload = new MemoryStream([0x0a, 0x01, 0x78, 0x12, 0x01, 0x79, 0x48, 0x01]);
        _contract = Serializer.Deserialize<ReactorDefinition>(payload);
    }

    void Because() => _result = Serializer.DeepClone(_contract).ToChronicle();

    [Fact] void should_read_the_flag_from_the_wire() => _contract.StartAtTail.ShouldBeTrue();
    [Fact] void should_carry_it_into_the_kernel() => _result.StartAtTail.ShouldBeTrue();
}
