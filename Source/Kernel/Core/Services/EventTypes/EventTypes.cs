// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Reactive;
using NJsonSchema;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventTypes"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypes"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grain references.</param>
internal sealed class EventTypes(IStorage storage, IGrainFactory grainFactory) : IEventTypes
{
    /// <inheritdoc/>
    public async Task Register(RegisterEventTypesRequest request)
    {
#if DEVELOPMENT
        var skipValidation = request.DisableValidation;
#else
        const bool skipValidation = false;
#endif
        var eventTypesStorage = storage.GetEventStore(request.EventStore).EventTypes;

        if (!skipValidation)
        {
            foreach (var eventType in request.Types)
            {
                ValidateMigrationChain(eventType.Type.Id, eventType.Type.Generation, eventType.Migrations);
                await ValidateSchemaNotChanged(eventType, eventTypesStorage);
            }
        }

        foreach (var eventType in request.Types)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema);
            var owner = (Concepts.Events.EventTypeOwner)(int)eventType.Owner;
            var source = (Concepts.Events.EventTypeSource)(int)eventType.Source;
            var eventTypeId = new EventTypeId(eventType.Type.Id);

            // Detect new generations before registration
            var newGenerations = new List<(EventTypeGeneration Generation, string Schema)>();
            foreach (var genDef in eventType.Generations)
            {
                if (!await eventTypesStorage.HasFor(eventTypeId, new EventTypeGeneration(genDef.Generation)))
                {
                    newGenerations.Add((new EventTypeGeneration(genDef.Generation), genDef.Schema));
                }
            }

            if (eventType.Generations.Count == 0 && !await eventTypesStorage.HasFor(eventTypeId, new EventTypeGeneration(eventType.Type.Generation)))
            {
                newGenerations.Add((new EventTypeGeneration(eventType.Type.Generation), eventType.Schema));
            }

            if (eventType.Migrations.Count > 0 || eventType.Generations.Count > 1)
            {
                // Register using full definition with all generations and migrations
                var generations = new List<Concepts.Events.EventTypeGenerationDefinition>();
                foreach (var genDef in eventType.Generations)
                {
                    var genSchema = await JsonSchema.FromJsonAsync(genDef.Schema);
                    generations.Add(new Concepts.Events.EventTypeGenerationDefinition(genDef.Generation, genSchema));
                }

                if (generations.Count == 0)
                {
                    generations.Add(new Concepts.Events.EventTypeGenerationDefinition(eventType.Type.ToChronicle().Generation, schema));
                }

                var filteredMigrations = eventType.Migrations.Where(m => m.FromGeneration != m.ToGeneration);
                var migrations = filteredMigrations.Select(m =>
                {
                    var upcastJson = string.IsNullOrEmpty(m.UpcastJmesPath)
                        ? new JsonObject()
                        : JsonNode.Parse(m.UpcastJmesPath)?.AsObject() ?? new JsonObject();
                    var downcastJson = string.IsNullOrEmpty(m.DowncastJmesPath)
                        ? new JsonObject()
                        : JsonNode.Parse(m.DowncastJmesPath)?.AsObject() ?? new JsonObject();

                    if (!skipValidation)
                    {
                        ValidateMigrationProperties(eventType.Type.Id, m, upcastJson, downcastJson, generations);
                    }

                    return new Concepts.Events.EventTypeMigrationDefinition(
                        m.FromGeneration,
                        m.ToGeneration,
                        [],
                        upcastJson,
                        downcastJson);
                }).ToList();

                var definition = new EventTypeDefinition(
                    eventType.Type.ToChronicle().Id,
                    owner,
                    eventType.Type.Tombstone,
                    generations,
                    migrations);

                await eventTypesStorage.Register(definition);
            }
            else
            {
                await eventTypesStorage.Register(
                    eventType.Type.ToChronicle(),
                    schema,
                    owner,
                    source);
            }

            // Append system events for new generations
            if (newGenerations.Count > 0)
            {
                var systemEventSequence = grainFactory.GetSystemEventSequence(request.EventStore);
                foreach (var (generation, genSchema) in newGenerations)
                {
                    await systemEventSequence.Append(
                        (EventSourceId)eventTypeId.Value,
                        new EventTypeGenerationAdded(eventTypeId, generation, genSchema));
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task RegisterSingle(RegisterSingleEventTypeRequest request)
    {
        var schema = await JsonSchema.FromJsonAsync(request.Type.Schema);
        await storage
            .GetEventStore(request.EventStore).EventTypes
            .Register(
                request.Type.Type.ToChronicle(),
                schema,
                (Concepts.Events.EventTypeOwner)(int)request.Type.Owner,
                (Concepts.Events.EventTypeSource)(int)request.Type.Source);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Events.EventType>> GetAll(GetAllEventTypesRequest request)
    {
        var eventTypes = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();
        return eventTypes.Select(_ => _.Type.ToContract());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeRegistration>> GetAllRegistrations(GetAllEventTypesRequest request)
    {
        var eventTypes = await storage.GetEventStore(request.EventStore).EventTypes.GetLatestForAllEventTypes();
        return eventTypes.Select(_ => new EventTypeRegistration
        {
            Type = _.Type.ToContract(),
            Owner = (Contracts.Events.EventTypeOwner)(int)_.Owner,
            Source = (Contracts.Events.EventTypeSource)(int)_.Source,
            Schema = _.Schema.ToJson()
        });
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<EventTypeRegistration>> ObserveAllRegistrations(GetAllEventTypesRequest request, CallContext context = default)
    {
        var eventStore = storage.GetEventStore(request.EventStore);
        return eventStore.EventTypes
            .ObserveLatestForAllEventTypes()
            .CompletedBy(context.CancellationToken)
            .Select(_ => _.Select(_ => new EventTypeRegistration
            {
                Type = _.Type.ToContract(),
                Owner = (Contracts.Events.EventTypeOwner)(int)_.Owner,
                Source = (Contracts.Events.EventTypeSource)(int)_.Source,
                Schema = _.Schema.ToJson()
            }).ToArray());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeRegistration>> GetAllGenerationsForEventType(GetEventTypeGenerationsRequest request)
    {
        var eventTypeId = new EventTypeId(request.EventTypeId);
        var eventType = new Concepts.Events.EventType(eventTypeId, EventTypeGeneration.First, false);
        var schemas = await storage.GetEventStore(request.EventStore).EventTypes.GetAllGenerationsForEventType(eventType);
        return schemas.Select(_ => new EventTypeRegistration
        {
            Type = _.Type.ToContract(),
            Owner = (Contracts.Events.EventTypeOwner)(int)_.Owner,
            Source = (Contracts.Events.EventTypeSource)(int)_.Source,
            Schema = _.Schema.ToJson()
        });
    }

    static void ValidateMigrationChain(string eventTypeId, uint currentGeneration, IList<Contracts.Events.EventTypeMigrationDefinition> migrations)
    {
        if (currentGeneration <= 1)
            return;

        var effectiveMigrations = migrations.Where(m => m.FromGeneration != m.ToGeneration).ToList();

        if (effectiveMigrations.Count == 0)
            throw new MissingEventTypeMigrators(eventTypeId, currentGeneration);

        if (!effectiveMigrations.Exists(m => m.FromGeneration == 1))
            throw new MissingFirstGenerationForEventType(eventTypeId, currentGeneration);

        for (uint from = 1; from < currentGeneration; from++)
        {
            if (!effectiveMigrations.Exists(m => m.FromGeneration == from))
                throw new MissingMigrationForEventTypeGeneration(eventTypeId, currentGeneration, from);
        }
    }

    static void ValidateMigrationProperties(
        string eventTypeId,
        Contracts.Events.EventTypeMigrationDefinition migration,
        JsonObject upcastJson,
        JsonObject downcastJson,
        List<Concepts.Events.EventTypeGenerationDefinition> generations)
    {
        var fromSchema = generations.FirstOrDefault(g => g.Generation == migration.FromGeneration)?.Schema;
        var toSchema = generations.FirstOrDefault(g => g.Generation == migration.ToGeneration)?.Schema;

        if (toSchema is not null)
        {
            ValidatePropertyKeys(eventTypeId, upcastJson, toSchema, migration.ToGeneration, "upcast");
        }

        if (fromSchema is not null)
        {
            ValidatePropertyKeys(eventTypeId, downcastJson, fromSchema, migration.FromGeneration, "downcast");
        }

        if (fromSchema is not null)
        {
            ValidateExpressionSources(eventTypeId, upcastJson, fromSchema, migration.FromGeneration, "upcast");
        }

        if (toSchema is not null)
        {
            ValidateExpressionSources(eventTypeId, downcastJson, toSchema, migration.ToGeneration, "downcast");
        }
    }

    static void ValidatePropertyKeys(string eventTypeId, JsonObject jmesPath, JsonSchema schema, uint generation, string direction)
    {
        var schemaProperties = schema.ActualProperties.Select(p => p.Key).ToHashSet();

        foreach (var property in jmesPath)
        {
            // DefaultValue introduces a brand-new property to the target generation.
            // The auto-generated schema for that generation may be empty, so skip validation.
            if (property.Value is JsonObject expr && expr.ContainsKey(WellKnownExpressions.DefaultValue))
            {
                continue;
            }

            if (!schemaProperties.Contains(property.Key))
            {
                throw new InvalidMigrationPropertyForEventType(eventTypeId, property.Key, generation, direction);
            }
        }
    }

    static void ValidateExpressionSources(string eventTypeId, JsonObject jmesPath, JsonSchema sourceSchema, uint sourceGeneration, string direction)
    {
        var schemaProperties = new HashSet<string>(sourceSchema.ActualProperties.Select(p => p.Key));

        foreach (var entry in jmesPath)
        {
            foreach (var prop in ExtractSourceProperties(entry.Value))
            {
                if (!schemaProperties.Contains(prop))
                {
                    throw new InvalidMigrationPropertyForEventType(eventTypeId, prop, sourceGeneration, direction);
                }
            }
        }
    }

    static IEnumerable<string> ExtractSourceProperties(JsonNode? value)
    {
        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue))
        {
            // JmesPath expression like "@.propertyName" — extract the property name
            if (stringValue.StartsWith("@."))
            {
                yield return stringValue[2..];
            }
        }
        else if (value is JsonObject obj && obj.Count == 1)
        {
            using var enumerator = obj.GetEnumerator();
            enumerator.MoveNext();
            var entry = enumerator.Current;
            switch (entry.Key)
            {
                case WellKnownExpressions.Rename when entry.Value is JsonValue renameVal && renameVal.TryGetValue<string>(out var oldName):
                    yield return oldName;
                    break;

                case WellKnownExpressions.Split when entry.Value is JsonObject splitConfig:
                    var source = splitConfig["source"]?.GetValue<string>();
                    if (source is not null)
                    {
                        yield return source;
                    }

                    break;

                case WellKnownExpressions.Combine when entry.Value is JsonArray combineArray:
                    foreach (var item in combineArray)
                    {
                        var propName = item?.GetValue<string>();
                        if (propName is not null)
                        {
                            yield return propName;
                        }
                    }

                    break;
            }
        }
    }

    static async Task ValidateSchemaNotChanged(EventTypeRegistration eventType, IEventTypesStorage eventTypesStorage)
    {
        var eventTypeId = new EventTypeId(eventType.Type.Id);

        foreach (var genDef in eventType.Generations)
        {
            var generation = new EventTypeGeneration(genDef.Generation);
            if (!await eventTypesStorage.HasFor(eventTypeId, generation))
            {
                continue;
            }

            var existingSchema = await eventTypesStorage.GetFor(eventTypeId, generation);
            var newSchema = await JsonSchema.FromJsonAsync(genDef.Schema);
            if (existingSchema.Schema.ToJson() != newSchema.ToJson())
            {
                throw new EventTypeSchemaChanged(eventType.Type.Id, genDef.Generation);
            }
        }
    }
}
