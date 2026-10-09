// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Sinks.for_SinkDefinitionConverters.when_round_tripping;

public class and_the_definition_is_legacy : Specification
{
    SinkDefinition _definition;
    SinkDefinition _result;

    void Establish() => _definition = new(new(Guid.NewGuid()), WellKnownSinkTypes.MongoDB);

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new LegacySink { ConfigurationId = _definition.Configuration, TypeId = _definition.Type });
        stream.Position = 0;
        _result = Serializer.Deserialize<Contracts.Sinks.SinkDefinition>(stream).ToChronicle();
    }

    [Fact] void should_preserve_fields_one_and_two() => _result.ShouldEqual(_definition);
    [Fact] void should_not_invent_event_metadata() => _result.EventSequence.ShouldBeNull();

    [ProtoContract]
    public class LegacySink
    {
        [ProtoMember(1)]
        public Guid ConfigurationId { get; set; }
        [ProtoMember(2)]
        public string TypeId { get; set; } = string.Empty;
    }
}
