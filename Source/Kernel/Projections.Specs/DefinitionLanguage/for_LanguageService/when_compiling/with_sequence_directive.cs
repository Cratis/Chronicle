// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class with_sequence_directive : for_LanguageService.given.a_language_service
{
    const string Definition = """
        projection SimulationProjection => Simulation
            sequence SomeNameOfSequence
            from SimulationAdded
                key $eventSourceId
                name = name
        """;
    ProjectionDefinition _definition;
    ReadModelDefinition _readModelDefinition;
    EventTypeSchema _eventTypeSchema;

    void Establish()
    {
        var schema = new JsonSchema { Title = "Simulation" };
        schema.Properties.Add("name", new JsonSchemaProperty { Type = JsonObjectType.String });
        _readModelDefinition = new ReadModelDefinition(
            new ReadModelIdentifier("Simulation"),
            new ReadModelName("Simulation"),
            new ReadModelDisplayName("Simulation"),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new Concepts.Sinks.SinkDefinition(
                new Concepts.Sinks.SinkConfigurationId(Guid.NewGuid()),
                Concepts.Sinks.WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = schema
            },
            []);

        var eventSchema = new JsonSchema { Title = "SimulationAdded" };
        eventSchema.Properties.Add("name", new JsonSchemaProperty { Type = JsonObjectType.String });
        _eventTypeSchema = new EventTypeSchema(
            (EventType)"SimulationAdded",
            EventTypeOwner.Client,
            EventTypeSource.Code,
            eventSchema);
    }

    void Because()
    {
        var result = _languageService.Compile(
            Definition,
            Concepts.Projections.ProjectionOwner.Client,
            [_readModelDefinition],
            [_eventTypeSchema]);
        _definition = result.Match(
            projectionDef => projectionDef,
            errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));
    }
    [Fact] void should_use_specified_sequence() => _definition.EventSequenceId.ShouldEqual(new EventSequenceId("SomeNameOfSequence"));
}
