// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Validates and shapes event type registrations on their way into storage.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for appending the system events new generations produce.</param>
/// <remarks>
/// This is where registration's rules live - the migration chain has to be unbroken, a schema may not change
/// under a generation that already exists, and a new generation is a fact the system event log records. The
/// artifacts state what is being asked for; this decides whether it is allowed and what it means.
/// </remarks>
public sealed class EventTypeRegistrar(IGrainFactory grainFactory)
{
    readonly ILogger<EventTypeRegistrar> _logger = NullLogger<EventTypeRegistrar>.Instance;

    /// <summary>
    /// Initializes a registrar with structured warnings for protection changes.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="logger">The logger.</param>
    public EventTypeRegistrar(IGrainFactory grainFactory, ILogger<EventTypeRegistrar> logger) : this(grainFactory)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers event types into an event store, validating them and recording what changed.
    /// </summary>
    /// <param name="eventStore">The event store to register into.</param>
    /// <param name="types">The registrations to write.</param>
    /// <param name="skipValidation">Whether to skip the migration and schema checks.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <param name="eventTypesCacheClient">Client for evicting the event type cache on every silo.</param>
    /// <param name="patternCapture">The <see cref="IPatternCapture"/> to keep observing every registered event type.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="MissingEventTypeMigrators">Thrown when a migrated event type declares no migrators.</exception>
    /// <exception cref="MissingFirstGenerationForEventType">Thrown when the migration chain has no first generation.</exception>
    /// <exception cref="MissingMigrationForEventTypeGeneration">Thrown when a generation in the chain has no migration.</exception>
    /// <exception cref="EventTypeSchemaChanged">Thrown when a stored generation's schema changed under it.</exception>
    public async Task Register(
        EventStoreName eventStore,
        IEnumerable<EventTypeRegistration> types,
        bool skipValidation,
        IStorage storage,
        IEventTypesCacheClient eventTypesCacheClient,
        IPatternCapture patternCapture)
    {
        var eventTypesStorage = storage.GetEventStore(eventStore).EventTypes;

        // The gRPC-deserialized sequence backing a repeated field is not guaranteed to support more than
        // one enumeration - and this method enumerates it twice, once to validate and once to build what
        // gets stored. Materializing it once up front is what makes the second pass see anything at all;
        // without this, a client's registration silently persisted nothing while still reporting success (#86).
        var typesList = types.ToList();

        // A client registers every event type it knows about in one call, and every check below - does the event
        // type exist, does this generation exist, has its schema changed - is answered by what is already stored.
        // Reading all of it once keeps registration at a couple of round trips instead of a handful per event type.
        var stored = (await eventTypesStorage.GetAllDefinitions()).ToDictionary(_ => _.Id);

        if (!skipValidation)
        {
            await ValidateAll(typesList, stored);
        }

        var (eventTypesToRegister, newGenerationsPerEventType) = await BuildRegistrations(typesList, stored, skipValidation);

        // Evict the event type cache on every silo whenever a registration actually changed the stored
        // representation - a new generation, or a different owner, source, or tombstone. Idempotent
        // re-registrations report no change, so client reconnects do not trigger cluster-wide eviction.
        var mutated = await eventTypesStorage.Register(eventTypesToRegister);
        LogAddedProtectionMetadata(eventTypesToRegister, stored);

        foreach (var eventTypeId in mutated)
        {
            var definition = await eventTypesStorage.GetDefinition(eventTypeId);
            await eventTypesStorage.RecordMigrationsVersion(eventTypeId, EventTypeMigrationsVersion.For(definition.Migrations), definition.Migrations);
            await eventTypesCacheClient.Invalidate(eventStore, eventTypeId);
        }

        await AppendSystemEventsForNewGenerations(eventStore, newGenerationsPerEventType);
        await CapturePatternsForNewEventTypes(patternCapture, eventStore, mutated);
    }

    /// <summary>
    /// Checks whether registrations would be accepted by <see cref="Register"/>, without writing anything.
    /// </summary>
    /// <param name="eventStore">The event store to check against.</param>
    /// <param name="types">The registrations to check.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <returns>One result per registration, in the order given.</returns>
    /// <remarks>
    /// Applies exactly the checks <see cref="Register"/> applies with validation on, against what is stored, and only reads:
    /// no event type is written, no system event is appended and no cache is evicted.
    /// </remarks>
    public async Task<IReadOnlyList<EventTypeCheckResult>> Check(
        EventStoreName eventStore,
        IEnumerable<EventTypeRegistration> types,
        IStorage storage)
    {
        var typesList = types.ToList();
        var stored = (await storage.GetEventStore(eventStore).EventTypes.GetAllDefinitions()).ToDictionary(_ => _.Id);
        var results = new List<EventTypeCheckResult>();

        foreach (var eventType in typesList)
        {
            results.Add(await CheckRegistration(eventType, StoredFor(stored, eventType)));
        }

        return results;
    }

    static async Task<EventTypeCheckResult> CheckRegistration(EventTypeRegistration eventType, EventTypeDefinition? storedDefinition)
    {
        try
        {
            return await CheckReadableRegistration(eventType, storedDefinition);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A schema or migration that cannot even be read must not abort the check for the other registrations.
            return EventTypeCheckResult.Reject(eventType.Type.Id, eventType.Type.Generation, $"The event type could not be processed: {ex.Message}");
        }
    }

    static async Task<EventTypeCheckResult> CheckReadableRegistration(EventTypeRegistration eventType, EventTypeDefinition? storedDefinition)
    {
        var id = eventType.Type.Id;
        var generation = eventType.Type.Generation;

        if (EventTypeMigrationValidation.FindChainProblem(id, generation, eventType.Migrations) is { } chainProblem)
        {
            return EventTypeCheckResult.Reject(id, generation, chainProblem.Message);
        }

        if (await EventTypeSchemaComparison.FindProblem(eventType, storedDefinition) is { } schemaProblem)
        {
            return EventTypeCheckResult.Reject(
                id,
                schemaProblem.Generation,
                new EventTypeSchemaChanged(id, schemaProblem.Generation).Message,
                schemaProblem.Difference.Keyword,
                schemaProblem.Difference.Path);
        }

        try
        {
            await CreateEventTypeToRegister(eventType, false, storedDefinition);
        }
        catch (EventTypeSchemaChanged ex)
        {
            return EventTypeCheckResult.Reject(id, generation, ex.Message, "protection", "$");
        }
        catch (InvalidMigrationPropertyForEventType ex)
        {
            return EventTypeCheckResult.Reject(id, generation, ex.Message);
        }

        return EventTypeCheckResult.Accept(id, generation);
    }

    /// <summary>
    /// Re-subscribes pattern capture when a registration actually introduced something new.
    /// </summary>
    /// <param name="patternCapture">The <see cref="IPatternCapture"/> to subscribe with.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/> that was registered against.</param>
    /// <param name="mutated">The event types the registration changed.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Pattern capture subscribes to the event types that exist at the time it subscribes, and a server starting
    /// against a store no client has connected to yet has none - so without this, a first run captures nothing at
    /// all until the next restart. Gated on the registration having changed something, so a client reconnecting and
    /// re-registering the same types does not re-subscribe on every connect.
    /// </remarks>
    static async Task CapturePatternsForNewEventTypes(IPatternCapture patternCapture, EventStoreName eventStore, IEnumerable<EventTypeId> mutated)
    {
        if (!mutated.Any())
        {
            return;
        }

        await patternCapture.SubscribeAcrossNamespaces(eventStore);
    }

    static async Task ValidateAll(List<EventTypeRegistration> types, Dictionary<EventTypeId, EventTypeDefinition> stored)
    {
        foreach (var eventType in types)
        {
            EventTypeMigrationValidation.ValidateChain(eventType.Type.Id, eventType.Type.Generation, eventType.Migrations);
            await ValidateSchemaNotChanged(eventType, StoredFor(stored, eventType));
        }
    }

    static async Task<(List<EventTypeToRegister> ToRegister, List<NewGenerations> NewGenerations)> BuildRegistrations(
        List<EventTypeRegistration> types,
        Dictionary<EventTypeId, EventTypeDefinition> stored,
        bool skipValidation)
    {
        var eventTypesToRegister = new List<EventTypeToRegister>();
        var newGenerationsPerEventType = new List<NewGenerations>();

        foreach (var eventType in types)
        {
            newGenerationsPerEventType.Add(GetNewGenerations(eventType, StoredFor(stored, eventType)));
            eventTypesToRegister.Add(await CreateEventTypeToRegister(eventType, skipValidation, StoredFor(stored, eventType)));
        }

        return (eventTypesToRegister, newGenerationsPerEventType);
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

    static async Task<EventTypeToRegister> CreateEventTypeToRegister(EventTypeRegistration eventType, bool skipValidation, EventTypeDefinition? storedDefinition)
    {
        var generations = await ParseGenerations(eventType);
        RefineAgainstStored(generations, storedDefinition, eventType.Type.Id);

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

        return new(
            definition,
            (Concepts.Events.EventTypeSource)(int)eventType.Source,
            (Concepts.Events.EventTypeVisibility)(int)eventType.Visibility,
            eventType.EventStore ?? string.Empty);
    }

    static async Task<List<Concepts.Events.EventTypeGenerationDefinition>> ParseGenerations(EventTypeRegistration eventType)
    {
        var generations = new List<Concepts.Events.EventTypeGenerationDefinition>();
        foreach (var genDef in eventType.Generations)
        {
            generations.Add(new Concepts.Events.EventTypeGenerationDefinition(genDef.Generation, await ParseSchema(genDef.Schema)));
        }

        if (generations.Count == 0)
        {
            generations.Add(new Concepts.Events.EventTypeGenerationDefinition(eventType.Type.ToChronicle().Generation, await ParseSchema(eventType.Schema)));
        }

        return generations;
    }

    static async Task<JsonSchema> ParseSchema(string schemaJson)
    {
        var schema = await JsonSchema.FromJsonAsync(schemaJson);
        schema.EnsureComplianceMetadata();
        return schema;
    }

    static void RefineAgainstStored(List<Concepts.Events.EventTypeGenerationDefinition> generations, EventTypeDefinition? storedDefinition, string eventTypeId)
    {
        for (var index = 0; index < generations.Count; index++)
        {
            var incoming = generations[index];
            var stored = storedDefinition?.Generations.FirstOrDefault(_ => _.Generation == incoming.Generation);
            if (stored is null)
            {
                continue;
            }

            var refined = stored.Schema.MorePrecise(incoming.Schema);
            if (!stored.Schema.HasCompatibleProtectionMetadata(refined))
            {
                throw new EventTypeSchemaChanged(eventTypeId, incoming.Generation.Value);
            }

            generations[index] = incoming with { Schema = refined };
        }
    }

    static JsonObject ParseJmesPath(string? jmesPath) =>
        string.IsNullOrEmpty(jmesPath)
            ? new JsonObject()
            : JsonNode.Parse(jmesPath)?.AsObject() ?? new JsonObject();

    static Concepts.Events.EventTypeMigrationDefinition CreateMigration(
        string eventTypeId,
        Contracts.Events.EventTypeMigrationDefinition migration,
        List<Concepts.Events.EventTypeGenerationDefinition> generations,
        bool skipValidation)
    {
        var definition = new Concepts.Events.EventTypeMigrationDefinition(
            migration.FromGeneration,
            migration.ToGeneration,
            [],
            ParseJmesPath(migration.UpcastJmesPath),
            ParseJmesPath(migration.DowncastJmesPath));

        if (!skipValidation)
        {
            EventTypeMigrationValidation.ValidateProperties(eventTypeId, definition, generations);
        }

        return definition;
    }

    static async Task ValidateSchemaNotChanged(EventTypeRegistration eventType, EventTypeDefinition? storedDefinition)
    {
        if (await EventTypeSchemaComparison.FindProblem(eventType, storedDefinition) is { } problem)
        {
            throw new EventTypeSchemaChanged(eventType.Type.Id, problem.Generation);
        }
    }

    static IEnumerable<(Concepts.Events.EventTypeGenerationDefinition Generation, string Path)> AddedProtectionPaths(
        EventTypeToRegister registration,
        Dictionary<EventTypeId, EventTypeDefinition> stored)
    {
        var previous = stored.GetValueOrDefault(registration.Definition.Id);
        foreach (var generation in registration.Definition.Generations)
        {
            var existing = previous?.Generations.FirstOrDefault(item => item.Generation == generation.Generation);
            if (existing is null)
            {
                continue;
            }

            foreach (var path in existing.Schema.AddedProtectionMetadataPaths(generation.Schema))
            {
                yield return (generation, path);
            }
        }
    }

    static async Task AppendFor(IEventSequence systemEventSequence, NewGenerations newGenerations)
    {
        var eventSourceId = (EventSourceId)newGenerations.EventTypeId.Value;

        if (!newGenerations.EventTypeAlreadyStored)
        {
            var firstGeneration = newGenerations.Generations.OrderBy(_ => _.Generation.Value).First();
            await systemEventSequence.Append(
                eventSourceId,
                new EventTypeAdded(newGenerations.EventTypeId, firstGeneration.Generation, firstGeneration.Schema));
            return;
        }

        foreach (var (generation, schema) in newGenerations.Generations)
        {
            await systemEventSequence.Append(
                eventSourceId,
                new EventTypeGenerationAdded(newGenerations.EventTypeId, generation, schema));
        }
    }

    void LogAddedProtectionMetadata(IEnumerable<EventTypeToRegister> registrations, Dictionary<EventTypeId, EventTypeDefinition> stored)
    {
        foreach (var registration in registrations)
        {
            foreach (var (generation, path) in AddedProtectionPaths(registration, stored))
            {
                _logger.ComplianceMetadataAddedToExistingGeneration(registration.Definition.Id.Value, generation.Generation.Value, path);
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
            await AppendFor(systemEventSequence, newGenerations);
        }
    }
}
