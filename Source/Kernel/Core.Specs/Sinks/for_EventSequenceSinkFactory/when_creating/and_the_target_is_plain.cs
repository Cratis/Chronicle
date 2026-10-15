// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSinkFactory.when_creating;

public class and_the_target_is_plain : Specification
{
    ISink _sink;
    SinkTypeId _type;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IGrainFactory>());
        services.AddSingleton(Substitute.For<IStorage>());
        services.AddSingleton(Substitute.For<IExpandoObjectConverter>());
        var factory = new EventSequenceSinkFactory(services.BuildServiceProvider());
        _type = factory.TypeId;
        var configuration = new EventSequenceSinkConfiguration(new EventType("Changed", EventTypeGeneration.First), null, true);
        var definition = new ReadModelDefinition(
            "target",
            "target",
            "Target",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.NotSet,
            ReadModelObserverIdentifier.Unspecified,
            new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence, configuration),
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromJson("{\"type\":\"object\"}") },
            []);
        _sink = factory.CreateFor("store", "tenant", definition);
    }

    [Fact] void should_serve_the_event_sequence_sink_type() => _type.ShouldEqual(WellKnownSinkTypes.EventSequence);
    [Fact] void should_create_an_event_sequence_sink() => _sink.ShouldBeOfExactType<EventSequenceSink>();
    [Fact] void should_identify_as_the_event_sequence_sink() => _sink.TypeId.ShouldEqual(WellKnownSinkTypes.EventSequence);
}
