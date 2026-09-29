// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling.given;

public class a_language_service_with_event_type_generations : for_LanguageService.given.a_language_service
{
    protected ReadModelDefinition _readModelDefinition;

    void Establish()
    {
        var readModelSchema = new JsonSchema { Title = "TestModel" };
        readModelSchema.Properties.Add("name", new JsonSchemaProperty { Type = JsonObjectType.String });
        _readModelDefinition = new ReadModelDefinition(
            new ReadModelIdentifier("TestModel"),
            new ReadModelContainerName("TestModel"),
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
    }

    protected static EventTypeSchema EventTypeSchemaFor(uint generation, JsonObjectType valueType)
    {
        var eventSchema = new JsonSchema { Title = "TestEvent" };
        eventSchema.Properties.Add("value", new JsonSchemaProperty { Type = valueType });
        return new EventTypeSchema(
            new EventType("TestEvent", generation),
            EventTypeOwner.Client,
            EventTypeSource.Code,
            eventSchema);
    }

    protected CompilerErrors Compile(string declaration, params EventTypeSchema[] eventTypeSchemas)
    {
        var result = _languageService.Compile(
            declaration,
            Concepts.Projections.ProjectionOwner.Client,
            [_readModelDefinition],
            eventTypeSchemas);

        return result.Match(_ => CompilerErrors.Empty, errors => errors);
    }
}
