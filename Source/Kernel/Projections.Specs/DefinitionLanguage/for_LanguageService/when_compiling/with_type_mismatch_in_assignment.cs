// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class with_type_mismatch_in_assignment : for_LanguageService.given.a_language_service
{
    const string Definition = """
        projection TestProjection => TestModel
            from TestEvent
                key $eventSourceId
                name = value
        """;

    ReadModelDefinition _readModelDefinition;
    EventTypeSchema _eventTypeSchema;
    CompilerErrors _errors;

    void Establish()
    {
        var readModelSchema = new JsonSchema { Title = "TestModel" };
        readModelSchema.Properties.Add("name", new JsonSchemaProperty { Type = JsonObjectType.String });
        _readModelDefinition = new ReadModelDefinition(
            new ReadModelIdentifier("TestModel"),
            new ReadModelName("TestModel"),
            new ReadModelDisplayName("TestModel"),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new Concepts.Sinks.SinkDefinition(
                new Concepts.Sinks.SinkConfigurationId(Guid.NewGuid()),
                Concepts.Sinks.WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = readModelSchema
            },
            []);

        var eventSchema = new JsonSchema { Title = "TestEvent" };
        eventSchema.Properties.Add("value", new JsonSchemaProperty { Type = JsonObjectType.Integer });
        _eventTypeSchema = new EventTypeSchema(
            (EventType)"TestEvent",
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

        _errors = result.Match(
            _ => CompilerErrors.Empty,
            errors => errors);
    }

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_type_mismatch() => _errors.Errors.Any(_ => _.Message.Contains("Type mismatch")).ShouldBeTrue();
}
