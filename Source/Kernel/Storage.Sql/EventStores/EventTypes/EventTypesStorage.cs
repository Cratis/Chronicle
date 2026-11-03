// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="logger">Optional <see cref="ILogger{EventTypesStorage}"/> for logging.</param>
public partial class EventTypesStorage(EventStoreName eventStore, IDatabase database, ILogger<EventTypesStorage>? logger = null) : IEventTypesStorage
{
    const int MaxMigrationVersionAttempts = 5;
    static readonly JsonSerializerOptions _migrationJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    readonly ILogger<EventTypesStorage> _logger = logger ?? NullLogger<EventTypesStorage>.Instance;
    readonly ConcurrentDictionary<EventTypeId, EventTypeDefinition> _definitionsByType = new();
    ConcurrentBag<EventType> _eventTypes = new();

    /// <inheritdoc/>
    public async Task<bool> Register(Concepts.Events.EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code, EventTypeVisibility visibility = EventTypeVisibility.Unspecified, string origin = "")
    {
        await using var scope = await database.EventStore(eventStore);

        var existingEventType = await scope.DbContext.EventTypes.FirstOrDefaultAsync(_ => _.Id == type.Id);

        // Unspecified means the client said nothing, which must not undo what a newer client declared.
        if (visibility == EventTypeVisibility.Unspecified && existingEventType is not null)
        {
            visibility = existingEventType.Visibility;
        }

        if (existingEventType is not null &&
            existingEventType.Owner == owner && existingEventType.Source == source && existingEventType.Tombstone == type.Tombstone &&
            existingEventType.Visibility == visibility && existingEventType.Origin == origin &&
            existingEventType.Schemas.TryGetValue(type.Generation, out var storedSchema) &&
            JsonSchemaCompatibilityExtensions.EqualsIgnoringTitles(storedSchema, schema.ToJson()))
        {
            return false;
        }

        var eventSchema = new EventTypeSchema(type, owner, source, schema, visibility, origin);
        if (_eventTypes.Any(_ => _.Id == type.Id))
        {
            _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != type.Id));
        }
        var eventType = PreserveStoredContent(existingEventType, eventSchema.ToSql());
        eventType.MigrationVersionsJson = existingEventType?.MigrationVersionsJson ?? "{}";
        _eventTypes.Add(eventType);

        await scope.DbContext.EventTypes.Upsert(eventType);
        _definitionsByType.TryRemove(eventType.Id, out _);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> Register(EventTypeDefinition definition)
    {
        await using var scope = await database.EventStore(eventStore);

        var existing = await scope.DbContext.EventTypes.FirstOrDefaultAsync(_ => _.Id == definition.Id);
        var migrationsJson = PrepareMigrations(existing, definition.Migrations);

        // Serialize the effective chain before ToSql's legacy fallback can turn a failed write into an empty list.
        var eventType = (definition with { Migrations = [] }).ToSql();
        eventType.MigrationsJson = migrationsJson;
        if (existing is not null)
        {
            // The full definition carries no visibility or origin, so what is already stored is kept.
            eventType.Visibility = existing.Visibility;
            eventType.Origin = existing.Origin;
            eventType.MigrationVersionsJson = existing.MigrationVersionsJson;

            eventType.Schemas = MergeSchemas(existing.Schemas, eventType.Schemas);

            if (existing.Owner == eventType.Owner && existing.Source == eventType.Source &&
                existing.Tombstone == eventType.Tombstone && existing.MigrationsJson == eventType.MigrationsJson &&
                existing.Schemas.Count == eventType.Schemas.Count &&
                eventType.Schemas.All(_ => existing.Schemas.TryGetValue(_.Key, out var storedSchema) && storedSchema == _.Value))
            {
                return false;
            }
        }

        _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != definition.Id));
        _eventTypes.Add(eventType);

        await scope.DbContext.EventTypes.Upsert(eventType);
        _definitionsByType.TryRemove(definition.Id, out _);
        return true;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions()
    {
        await using var scope = await database.EventStore(eventStore);
        var eventTypes = await scope.DbContext.EventTypes.ToListAsync();
        return eventTypes.ConvertAll(static eventType => eventType.ToDefinition());
    }

    /// <inheritdoc/>
    public async Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId)
    {
        if (_definitionsByType.TryGetValue(eventTypeId, out var cached))
        {
            return cached;
        }

        var eventType = await GetSpecificEventType(eventTypeId);
        if (eventType is null)
        {
            var schema = await GetFor(eventTypeId);
            return new EventTypeDefinition(
                eventTypeId,
                EventTypeOwner.None,
                false,
                [new EventTypeGenerationDefinition(schema.Type.Generation, schema.Schema)],
                []);
        }

        return _definitionsByType.GetOrAdd(eventTypeId, static (_, document) => document.ToDefinition(), eventType);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes()
    {
        await using var scope = await database.EventStore(eventStore);
        var eventTypes = await scope.DbContext.EventTypes.ToListAsync();
        return eventTypes.ConvertAll(_ => _.ToKernelForLatestGeneration());
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() => LiveQuery.Observe(GetLatestForAllEventTypes, database, "event-types");

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds)
    {
        var ids = eventTypeIds.ToList();
        await using var scope = await database.EventStore(eventStore);
        var eventTypes = await scope.DbContext.EventTypes.Where(e => ids.Contains(e.Id)).ToListAsync();
        return eventTypes.Select(_ => _.ToKernel());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<Concepts.Events.EventType> eventTypes)
    {
        var eventTypesList = eventTypes.ToList();
        var ids = eventTypesList.ConvertAll(et => et.Id);
        await using var scope = await database.EventStore(eventStore);
        var storedTypes = await scope.DbContext.EventTypes.Where(e => ids.Contains(e.Id)).ToListAsync();
        var storedTypeMap = storedTypes.ToDictionary(s => s.Id);
        return eventTypesList
            .Where(et => storedTypeMap.ContainsKey(et.Id))
            .Select(et => storedTypeMap[et.Id].ToKernel(et.Generation));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(Concepts.Events.EventType eventType)
    {
        var storedEventType = await GetSpecificEventType(eventType.Id);
        if (storedEventType is null)
        {
            return [];
        }

        return storedEventType.Schemas.Select(kvp => storedEventType.ToKernel(new EventTypeGeneration(kvp.Key)));
    }

    /// <inheritdoc/>
    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = null)
    {
        generation ??= EventTypeGeneration.First;
        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generation)))
        {
            return _eventTypes.First(_ => _.Id == type && _.Schemas.ContainsKey(generation)).ToKernel(generation);
        }
        var eventType = await GetSpecificEventType(type) ?? throw new UnknownEventType(eventStore, type);
        if (!eventType.Schemas.ContainsKey(generation))
        {
            throw new MissingEventSchemaForEventType(
                eventStore,
                type,
                generation);
        }

        return eventType.ToKernel(generation);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = null)
    {
        generation ??= EventTypeGeneration.First;
        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generation)))
        {
            // Verify the cache is consistent with the database. After an external database
            // reset (e.g. between integration test classes), the cache may still hold entries
            // for event types that no longer exist in the backing store.
            var dbEventType = await GetSpecificEventType(type);
            if (dbEventType?.Schemas.ContainsKey(generation) != true)
            {
                _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != type));
                return false;
            }

            return true;
        }

        var eventType = await GetSpecificEventType(type);
        return eventType?.Schemas.ContainsKey(generation) ?? false;
    }

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId)
    {
        _definitionsByType.TryRemove(eventTypeId, out _);
        if (_eventTypes.Any(_ => _.Id == eventTypeId))
        {
            _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != eventTypeId));
        }
    }

    static EventType PreserveStoredContent(EventType? existing, EventType incoming)
    {
        if (existing is null)
        {
            return incoming;
        }

        // A simple registration carries one schema and no migration knowledge.
        incoming.Schemas = MergeSchemas(existing.Schemas, incoming.Schemas);
        incoming.MigrationsJson = existing.MigrationsJson;
        return incoming;
    }

    static Dictionary<uint, string> MergeSchemas(IDictionary<uint, string> stored, IDictionary<uint, string> incoming)
    {
        var schemas = new Dictionary<uint, string>(stored);

        // Omitted schemas and stored CLR titles survive when the structural schema is unchanged.
        foreach (var (generation, schema) in incoming.Where(_ => !stored.TryGetValue(_.Key, out var previous) ||
            !JsonSchemaCompatibilityExtensions.EqualsIgnoringTitles(previous, _.Value)))
        {
            schemas[generation] = schema;
        }
        return schemas;
    }

    static string PrepareMigrations(EventType? existing, IEnumerable<EventTypeMigrationDefinition> incoming)
    {
        var stored = existing?.ToDefinition().Migrations.ToList() ?? [];
        var migrations = MergeMigrations(stored, incoming);
        return existing is not null && MigrationsEqual(stored, migrations)
            ? existing.MigrationsJson
            : JsonSerializer.Serialize(migrations, _migrationJsonOptions);
    }

    static List<EventTypeMigrationDefinition> MergeMigrations(IEnumerable<EventTypeMigrationDefinition> stored, IEnumerable<EventTypeMigrationDefinition> incoming)
    {
        var migrations = stored.ToDictionary(_ => (_.FromGeneration, _.ToGeneration));
        foreach (var migration in incoming)
        {
            migrations[(migration.FromGeneration, migration.ToGeneration)] = migration;
        }
        return migrations.Values.OrderBy(_ => _.FromGeneration.Value).ThenBy(_ => _.ToGeneration.Value).ToList();
    }

    static bool MigrationsEqual(List<EventTypeMigrationDefinition> stored, List<EventTypeMigrationDefinition> incoming)
    {
        if (stored.Count != incoming.Count)
        {
            return false;
        }
        var byEndpoint = stored.ToDictionary(_ => (_.FromGeneration, _.ToGeneration));
        return incoming.TrueForAll(migration => byEndpoint.TryGetValue((migration.FromGeneration, migration.ToGeneration), out var previous) &&
            JsonNode.DeepEquals(JsonSerializer.SerializeToNode(previous, _migrationJsonOptions), JsonSerializer.SerializeToNode(migration, _migrationJsonOptions)));
    }

    async Task<EventType?> GetSpecificEventType(EventTypeId eventTypeId)
    {
        var eventTypeIdValue = eventTypeId.Value;
        await using var scope = await database.EventStore(eventStore);
        return await scope.DbContext.EventTypes.FirstOrDefaultAsync(e => e.Id == eventTypeIdValue);
    }
}
