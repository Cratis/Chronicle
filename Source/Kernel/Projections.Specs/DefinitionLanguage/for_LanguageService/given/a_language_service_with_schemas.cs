// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.given;

public abstract class a_language_service_with_schemas<TReadModel> : Specification
    where TReadModel : class
{
    protected ILanguageService _languageService;
    protected ProjectionId _projectionId;
    protected ReadModelDefinition _readModelDefinition;
    protected List<EventTypeSchema> _eventTypeSchemas;

    protected virtual IEnumerable<Type> EventTypes => [];

    void Establish()
    {
        _languageService = new LanguageService(new Generator());
        _projectionId = new ProjectionId(Guid.NewGuid().ToString());
        _readModelDefinition = CreateReadModelDefinition<TReadModel>();
        _eventTypeSchemas = CreateEventTypeSchemas(EventTypes).ToList();
    }

    protected ProjectionDefinition CompileGenerateAndRecompile(string definition)
    {
        var result = _languageService.Compile(
            definition,
            ProjectionOwner.Client,
            [_readModelDefinition],
            _eventTypeSchemas);
        var compiled = result.Match(
            projectionDef => projectionDef,
            errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));

        var generated = _languageService.Generate(compiled, _readModelDefinition);

        var recompileResult = _languageService.Compile(
            generated,
            ProjectionOwner.Client,
            [_readModelDefinition],
            _eventTypeSchemas);
        return recompileResult.Match(
            projectionDef => projectionDef,
            errors => throw new InvalidOperationException($"Re-compilation of generated DSL failed: {string.Join(", ", errors.Errors)}\n\nGenerated DSL was:\n{generated}"));
    }

    static ReadModelDefinition CreateReadModelDefinition<T>()
        where T : class
    {
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }
        };
        var schema = JsonSchema.FromType<T>(settings);
        var name = typeof(T).Name;

        return new ReadModelDefinition(
            new ReadModelIdentifier(name),
            new ReadModelName(name),
            ReadModelOwner.Client,
            new Concepts.Sinks.SinkDefinition(
                new Concepts.Sinks.SinkConfigurationId(Guid.NewGuid()),
                Concepts.Sinks.WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = schema
            },
            []);
    }

    static IEnumerable<EventTypeSchema> CreateEventTypeSchemas(IEnumerable<Type> eventTypes)
    {
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }
        };
        foreach (var eventType in eventTypes)
        {
            var schema = JsonSchema.FromType(eventType, settings);
            yield return new EventTypeSchema(
                (EventType)eventType.Name,
                EventTypeOwner.Client,
                EventTypeSource.Code,
                schema);
        }
    }
}
