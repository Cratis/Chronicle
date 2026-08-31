// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
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
/// <param name="eventTypesCacheClient">Client for evicting event type caches on every silo when a registration changes one.</param>
/// <param name="patternCapture"><see cref="IPatternCapture"/> to keep observing every registered event type.</param>
internal sealed class EventTypes(
    IStorage storage,
    IGrainFactory grainFactory,
    Cratis.Chronicle.EventTypes.IEventTypesCacheClient eventTypesCacheClient,
    IPatternCapture patternCapture) : IEventTypes
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

        // A client registers every event type it knows about in one call, and every check below - does the event
        // type exist, does this generation exist, has its schema changed - is answered by what is already stored.
        // Reading all of it once keeps registration at a couple of round trips instead of a handful per event type.
        var stored = (await eventTypesStorage.GetAllDefinitions()).ToDictionary(_ => _.Id);

        if (!skipValidation)
        {
            foreach (var eventType in request.Types)
            {
                ValidateMigrationChain(eventType.Type.Id, eventType.Type.Generation, eventType.Migrations);
                await ValidateSchemaNotChanged(eventType, StoredFor(stored, eventType));
            }
        }

        var eventTypesToRegister = new List<EventTypeToRegister>();
        var newGenerationsPerEventType = new List<NewGenerations>();

        foreach (var eventType in request.Types)
        {
            newGenerationsPerEventType.Add(GetNewGenerations(eventType, StoredFor(stored, eventType)));
            eventTypesToRegister.Add(await CreateEventTypeToRegister(eventType, skipValidation));
        }

        // Evict the event type cache on every silo whenever a registration actually changed the stored
        // representation - a new generation, or a different owner, source, or tombstone. Idempotent
        // re-registrations report no change, so client reconnects do not trigger cluster-wide eviction.
        var mutated = await eventTypesStorage.Register(eventTypesToRegister);

        foreach (var eventTypeId in mutated)
        {
            await eventTypesCacheClient.Invalidate(request.EventStore, eventTypeId);
        }

        await AppendSystemEventsForNewGenerations(request.EventStore, newGenerationsPerEventType);
        await CapturePatternsForNewEventTypes(request.EventStore, mutated);
    }

    /// <inheritdoc/>
    public async Task RegisterSingle(RegisterSingleEventTypeRequest request)
    {
        var chronicleType = request.Type.Type.ToChronicle();
        var schema = await JsonSchema.FromJsonAsync(request.Type.Schema);
        var mutated = await storage
            .GetEventStore(request.EventStore).EventTypes
            .Register(
                chronicleType,
                schema,
                (Concepts.Events.EventTypeOwner)(int)request.Type.Owner,
                (Concepts.Events.EventTypeSource)(int)request.Type.Source);

        if (mutated)
        {
            await eventTypesCacheClient.Invalidate(request.EventStore, chronicleType.Id);
            await patternCapture.SubscribeAcrossNamespaces(request.EventStore);
        }
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

    static EventTypeDefinition? StoredFor(Dictionary<EventTypeId, EventTypeDefinition> stored, EventTypeRegistration eventType) =>
        stored.GetValueOrDefault(new EventTypeId(eventType.Type.Id));

    static NewGenerations GetNewGenerations(EventTypeRegistration eventType, EventTypeDefinition? storedDefinition)
    {
        var eventTypeId = new EventTypeId(eventType.Type.Id);
        var storedGenerations = storedDefinition?.Generations.Select(_ => _.Generation).ToHashSet() ?? [];

        var generations = eventType.Generations
            .Select(_ => (Generation: new EventTypeGeneration(_.Generation), _.Schema))
            .Where(_ => !storedGenerations.Contains(_.Generation))
            .ToList();

        if (eventType.Generations.Count == 0 && !storedGenerations.Contains(new EventTypeGeneration(eventType.Type.Generation)))
        {
            generations.Add((new EventTypeGeneration(eventType.Type.Generation), eventType.Schema));
        }

        return new(eventTypeId, storedDefinition is not null, generations);
    }

    static async Task<EventTypeToRegister> CreateEventTypeToRegister(EventTypeRegistration eventType, bool skipValidation)
    {
        var generations = new List<Concepts.Events.EventTypeGenerationDefinition>();
        foreach (var genDef in eventType.Generations)
        {
            var genSchema = await JsonSchema.FromJsonAsync(genDef.Schema);
            genSchema.EnsureComplianceMetadata();
            generations.Add(new Concepts.Events.EventTypeGenerationDefinition(genDef.Generation, genSchema));
        }

        if (generations.Count == 0)
        {
            var schema = await JsonSchema.FromJsonAsync(eventType.Schema);
            schema.EnsureComplianceMetadata();
            generations.Add(new Concepts.Events.EventTypeGenerationDefinition(eventType.Type.ToChronicle().Generation, schema));
        }

        var migrations = eventType.Migrations
            .Where(m => m.FromGeneration != m.ToGeneration)
            .Select(m => CreateMigration(eventType.Type.Id, m, generations, skipValidation))
            .ToList();

        var definition = new EventTypeDefinition(
            eventType.Type.ToChronicle().Id,
            (Concepts.Events.EventTypeOwner)(int)eventType.Owner,
            eventType.Type.Tombstone,
            generations,
            migrations);

        return new(definition, (Concepts.Events.EventTypeSource)(int)eventType.Source);
    }

    static Concepts.Events.EventTypeMigrationDefinition CreateMigration(
        string eventTypeId,
        Contracts.Events.EventTypeMigrationDefinition migration,
        List<Concepts.Events.EventTypeGenerationDefinition> generations,
        bool skipValidation)
    {
        var upcastJson = string.IsNullOrEmpty(migration.UpcastJmesPath)
            ? new JsonObject()
            : JsonNode.Parse(migration.UpcastJmesPath)?.AsObject() ?? new JsonObject();
        var downcastJson = string.IsNullOrEmpty(migration.DowncastJmesPath)
            ? new JsonObject()
            : JsonNode.Parse(migration.DowncastJmesPath)?.AsObject() ?? new JsonObject();

        if (!skipValidation)
        {
            ValidateMigrationProperties(eventTypeId, migration, upcastJson, downcastJson, generations);
        }

        return new Concepts.Events.EventTypeMigrationDefinition(
            migration.FromGeneration,
            migration.ToGeneration,
            [],
            upcastJson,
            downcastJson);
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

                case WellKnownExpressions.Combine when entry.Value is JsonObject combineConfig:
                    if (combineConfig["sources"] is JsonArray combineArray)
                    {
                        foreach (var item in combineArray)
                        {
                            var propName = item?.GetValue<string>();
                            if (propName is not null)
                            {
                                yield return propName;
                            }
                        }
                    }

                    break;
            }
        }
    }

    static async Task ValidateSchemaNotChanged(EventTypeRegistration eventType, EventTypeDefinition? storedDefinition)
    {
        if (storedDefinition is null)
        {
            return;
        }

        foreach (var genDef in eventType.Generations)
        {
            var generation = new EventTypeGeneration(genDef.Generation);
            var existingGeneration = storedDefinition.Generations.FirstOrDefault(_ => _.Generation == generation);
            if (existingGeneration is null)
            {
                continue;
            }

            var newSchema = await JsonSchema.FromJsonAsync(genDef.Schema);

            // Storage applies EnsureComplianceMetadata() when deserializing a stored schema
            // (in EventTypeConverters.ToDefinition). Apply the same transformation to the incoming
            // schema so both sides go through identical normalization before comparison.
            newSchema.EnsureComplianceMetadata();

            // Compare for compatibility rather than equality. Two differences cannot change what an already
            // stored payload means and must not be rejected: a nullability marker ('?' on a format value), which
            // a Chronicle upgrade can introduce on a schema stored before the marker existed, and an enumeration
            // that only gained members or had members renamed - neither moves an existing member off the
            // underlying value a stored payload carries. Everything else, including a member that disappeared or
            // was renumbered, still needs a new generation.
            if (!existingGeneration.Schema.IsCompatibleWith(newSchema))
            {
                throw new EventTypeSchemaChanged(eventType.Type.Id, genDef.Generation);
            }
        }
    }

    async Task AppendSystemEventsForNewGenerations(EventStoreName eventStore, IEnumerable<NewGenerations> newGenerationsPerEventType)
    {
        var withNewGenerations = newGenerationsPerEventType.Where(_ => _.Generations.Count > 0).ToList();

        if (withNewGenerations.Count == 0)
        {
            return;
        }

        var systemEventSequence = grainFactory.GetSystemEventSequence(eventStore);

        foreach (var newGenerations in withNewGenerations)
        {
            var eventSourceId = (EventSourceId)newGenerations.EventTypeId.Value;

            if (!newGenerations.EventTypeAlreadyStored)
            {
                var firstGeneration = newGenerations.Generations.OrderBy(_ => _.Generation.Value).First();
                await systemEventSequence.Append(
                    eventSourceId,
                    new EventTypeAdded(newGenerations.EventTypeId, firstGeneration.Generation, firstGeneration.Schema));
                continue;
            }

            foreach (var (generation, schema) in newGenerations.Generations)
            {
                await systemEventSequence.Append(
                    eventSourceId,
                    new EventTypeGenerationAdded(newGenerations.EventTypeId, generation, schema));
            }
        }
    }

    /// <summary>
    /// Re-subscribes pattern capture when a registration actually introduced something new.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> that was registered against.</param>
    /// <param name="mutated">The event types the registration changed.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Pattern capture subscribes to the event types that exist at the time it subscribes, and a server starting
    /// against a store no client has connected to yet has none - so without this, a first run captures nothing at
    /// all until the next restart. Gated on the registration having changed something, so a client reconnecting and
    /// re-registering the same types does not re-subscribe on every connect.
    /// </remarks>
    async Task CapturePatternsForNewEventTypes(EventStoreName eventStore, IEnumerable<EventTypeId> mutated)
    {
        if (!mutated.Any())
        {
            return;
        }

        await patternCapture.SubscribeAcrossNamespaces(eventStore);
    }

    /// <summary>
    /// Represents the generations of an event type that are not yet stored, and whether the event type itself is
    /// already stored - which is what decides between an <see cref="EventTypeAdded"/> and an
    /// <see cref="EventTypeGenerationAdded"/> system event.
    /// </summary>
    /// <param name="EventTypeId">The <see cref="EventTypeId"/> the generations belong to.</param>
    /// <param name="EventTypeAlreadyStored">Whether the event type is already stored.</param>
    /// <param name="Generations">The generations that are not yet stored, with their schema as it came in.</param>
    sealed record NewGenerations(
        EventTypeId EventTypeId,
        bool EventTypeAlreadyStored,
        IReadOnlyList<(EventTypeGeneration Generation, string Schema)> Generations);
}
