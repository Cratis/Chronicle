// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_ReactorDefinitionConverters.when_converting_to_chronicle;

public class without_start_at_tail : Specification
{
    ReactorDefinition _contract;
    Concepts.Observation.Reactors.ReactorDefinition _result;

    void Establish()
    {
        // A client that predates the field never sends it.
        using var payload = new MemoryStream([0x0a, 0x01, 0x78, 0x12, 0x01, 0x79]);
        _contract = Serializer.Deserialize<ReactorDefinition>(payload);
    }

    void Because() => _result = Serializer.DeepClone(_contract).ToChronicle();

    [Fact] void should_start_from_the_beginning_as_before() => _result.StartAtTail.ShouldBeFalse();
}
