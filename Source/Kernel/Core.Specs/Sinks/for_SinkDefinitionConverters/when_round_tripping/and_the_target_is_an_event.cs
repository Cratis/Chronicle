// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Sinks;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Sinks.for_SinkDefinitionConverters.when_round_tripping;

public class and_the_target_is_an_event : Specification
{
    SinkDefinition _definition;
    SinkDefinition _result;
    Contracts.Sinks.SinkDefinition _wire;

    void Establish() => _definition = new(new(Guid.NewGuid()), WellKnownSinkTypes.EventSequence, new(new EventType("PublicStateChanged", new EventTypeGeneration(7)), "public-feed"));

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, _definition.ToContract());
        stream.Position = 0;
        _wire = Serializer.Deserialize<Contracts.Sinks.SinkDefinition>(stream);
        _result = _wire.ToChronicle();
    }

    [Fact] void should_preserve_configuration_identity() => _result.Configuration.ShouldEqual(_definition.Configuration);
    [Fact] void should_preserve_the_exact_event_type_and_generation() => _result.EventSequence!.EventType.ShouldEqual(_definition.EventSequence!.EventType);
    [Fact] void should_preserve_the_destination() => _result.EventSequence!.Destination.ShouldEqual(new EventSequenceId("public-feed"));
    [Fact] void should_use_a_distinct_sink_type_for_old_kernels() => _wire.TypeId.ShouldEqual(WellKnownSinkTypes.EventSequence.Value);
}
