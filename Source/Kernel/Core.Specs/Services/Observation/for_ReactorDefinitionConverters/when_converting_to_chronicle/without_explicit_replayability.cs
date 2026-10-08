// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_ReactorDefinitionConverters.when_converting_to_chronicle;

public class without_explicit_replayability : Specification
{
    ReactorDefinition _omitted;
    ReactorDefinition _false;
    Concepts.Observation.Reactors.ReactorDefinition _omittedResult;
    Concepts.Observation.Reactors.ReactorDefinition _falseResult;

    void Establish()
    {
        using var omittedPayload = new MemoryStream([0x0a, 0x01, 0x78, 0x12, 0x01, 0x79]);
        using var falsePayload = new MemoryStream([0x0a, 0x01, 0x78, 0x12, 0x01, 0x79, 0x20, 0x00]);
        _omitted = Serializer.Deserialize<ReactorDefinition>(omittedPayload);
        _false = Serializer.Deserialize<ReactorDefinition>(falsePayload);
    }

    void Because()
    {
        _omittedResult = _omitted.ToChronicle();
        _falseResult = _false.ToChronicle();
    }

    [Fact] void should_keep_true_for_an_omitted_legacy_field() => _omittedResult.IsReplayable.ShouldBeTrue();
    [Fact] void should_keep_false_for_an_explicit_legacy_field() => _falseResult.IsReplayable.ShouldBeFalse();
}
