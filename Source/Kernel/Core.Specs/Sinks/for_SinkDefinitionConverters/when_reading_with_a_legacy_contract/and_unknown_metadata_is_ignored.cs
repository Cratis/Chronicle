// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Sinks;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Sinks.for_SinkDefinitionConverters.when_reading_with_a_legacy_contract;

public class and_unknown_metadata_is_ignored : Specification
{
    when_round_tripping.and_the_definition_is_legacy.LegacySink _legacy;

    void Because()
    {
        var definition = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence, new(new EventType("PublicStateChanged", EventTypeGeneration.First)));
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, definition.ToContract());
        stream.Position = 0;
        _legacy = Serializer.Deserialize<when_round_tripping.and_the_definition_is_legacy.LegacySink>(stream);
    }

    [Fact] void should_retain_the_unknown_sink_type_even_without_metadata() => _legacy.TypeId.ShouldEqual(WellKnownSinkTypes.EventSequence.Value);
    [Fact] void should_not_select_mongodb() => (_legacy.TypeId == WellKnownSinkTypes.MongoDB.Value).ShouldBeFalse();
    [Fact] void should_not_select_sql() => (_legacy.TypeId == WellKnownSinkTypes.SQL.Value).ShouldBeFalse();
}
