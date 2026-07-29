// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Reactive;
using Cratis.Strings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesStorage"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypesStorage"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="EventStoreName"/> the storage is for.</param>
/// <param name="sharedDatabase">The <see cref="IEventStoreDatabase"/>.</param>
/// <param name="logger">Logger for logging.</param>
public class EventTypesStorage(
    EventStoreName eventStore,
    IEventStoreDatabase sharedDatabase,
    ILogger<EventTypesStorage> logger) : IEventTypesStorage
{
    /// <summary>
    /// Caches the parsed <see cref="EventTypeSchema"/> per event type and generation, so the append and
    /// event-read paths reuse the same instance instead of reparsing a fresh <see cref="JsonSchema"/> whose
    /// lazy caches never warm.
    /// </summary>
    /// <remarks>
    /// Concurrency: <see cref="ConcurrentDictionary{TKey,TValue}"/> with GetOrAdd semantics (mirroring
    /// <c>EventTypes</c> in the kernel core) - a race may parse twice but only one instance is ever stored and
    /// shared; no per-call locking is taken. Growth is bounded by the number of registered event types times
    /// their generations; generations are immutable once written, so an entry is only removed when a new
    /// generation for its type is registered (see <see cref="Invalidate"/>).
    /// </remarks>
    readonly ConcurrentDictionary<(EventTypeId Id, EventTypeGeneration Generation), EventTypeSchema> _schemasByTypeAndGeneration = new();

    /// <summary>
    /// Caches the parsed <see cref="EventTypeDefinition"/> per event type. Evicted whenever the type is
    /// registered, since a new generation changes the aggregated definition. Growth is bounded by the number
    /// of registered event types.
    /// </summary>
    readonly ConcurrentDictionary<EventTypeId, EventTypeDefinition> _definitionsByType = new();

    /// <inheritdoc/>
    public async Task<bool> Register(Concepts.Events.EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code)
    {
        logger.Registering(type.Id, type.Generation, eventStore);

        var generationKey = type.Generation.ToString();
        var schemaDocument = BsonDocument.Parse(schema.ToJson());

        // Merge the incoming generation into the stored document by setting only its schema entry and the
        // metadata fields. Other generations - which another silo may have registered - are left untouched,
        // so a rolling deploy never clobbers generations this silo does not know about.
        var update = Builders<EventType>.Update
            .Set(_ => _.Owner, owner)
            .Set(_ => _.Source, source)
            .Set(_ => _.Tombstone, type.Tombstone)
            .Set($"{nameof(EventType.Schemas).ToCamelCase()}.{generationKey}", schemaDocument);

        var result = await GetCollection().UpdateOneAsync(
            _ => _.Id == type.Id,
            update,
            new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);

        return InvalidateIfMutated(type.Id, result.ModifiedCount > 0 || result.UpsertedId is not null);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes()
    {
        using var result = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        var schemas = await result.ToListAsync();
        return schemas.Select(_ => _.ToKernel());
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() =>
        new TransformingSubject<IEnumerable<EventType>, IEnumerable<EventTypeSchema>>(
            GetCollection().Observe(),
            _ => _.Select(_ => _.ToKernel()));

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(Concepts.Events.EventType eventType)
    {
        var collection = GetCollection();
        var filter = GetFilterForSpecificEventType(eventType.Id);
        using var result = await collection.FindAsync(filter).ConfigureAwait(false);
        var schemas = await result.ToListAsync();
        return schemas.Select(_ => _.ToKernel());
    }

    /// <inheritdoc/>
    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        generation ??= EventTypeGeneration.First;
        var key = (type, generation);

        if (_schemasByTypeAndGeneration.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var filter = GetFilterForSpecificEventType(type);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var schemas = await result.ToListAsync();

        if (schemas.Count == 0)
        {
            throw new MissingEventSchemaForEventType(
                eventStore,
                type,
                generation);
        }

        return _schemasByTypeAndGeneration.GetOrAdd(key, schemas[0].ToKernel(generation));
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        generation ??= EventTypeGeneration.First;

        if (_schemasByTypeAndGeneration.ContainsKey((type, generation)))
        {
            return true;
        }

        var filter = GetFilterForSpecificEventType(type);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var eventTypes = await result.ToListAsync();

        return eventTypes.Count > 0 && eventTypes[0].Schemas.ContainsKey(generation.ToString());
    }

    /// <inheritdoc/>
    public async Task<bool> Register(EventTypeDefinition definition)
    {
        logger.Registering(definition.Id, EventTypeGeneration.First, eventStore);

        var mongoEventType = definition.ToMongoDB();

        var result = await GetCollection().ReplaceOneAsync(
            _ => _.Id == definition.Id,
            mongoEventType,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);

        return InvalidateIfMutated(definition.Id, result.ModifiedCount > 0 || result.UpsertedId is not null);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Collapses the whole batch into one read and one write: the current documents for every identifier are read in
    /// a single query, compared in memory to work out which event types actually change anything, and only those are
    /// written - as a single unordered bulk write. Every event type merges its generations into the stored document,
    /// leaving generations another silo may have registered untouched, exactly as the single-event-type registration
    /// does. Migrations are only written when the event type carries them, so a client that does not know about an
    /// event type's migrations cannot erase them.
    /// </remarks>
    public async Task<IEnumerable<EventTypeId>> Register(IEnumerable<EventTypeToRegister> eventTypes)
    {
        var all = eventTypes.ToList();

        if (all.Count == 0)
        {
            return [];
        }

        var collection = GetCollection();
        var ids = all.ConvertAll(_ => _.Definition.Id);
        using var cursor = await collection.FindAsync(Builders<EventType>.Filter.In(_ => _.Id, ids)).ConfigureAwait(false);
        var stored = (await cursor.ToListAsync()).ToDictionary(_ => _.Id);

        var writes = new List<WriteModel<EventType>>();
        var mutated = new List<EventTypeId>();

        foreach (var eventType in all)
        {
            var definition = eventType.Definition;
            var schemas = definition.Generations.ToDictionary(
                _ => _.Generation.ToString(),
                _ => BsonDocument.Parse(_.Schema.ToJson()));
            var migrations = definition.Migrations.Select(_ => new EventTypeMigration(
                _.FromGeneration,
                _.ToGeneration,
                BsonDocument.Parse(_.UpcastJmesPath?.ToJsonString() ?? "{}"),
                BsonDocument.Parse(_.DowncastJmesPath?.ToJsonString() ?? "{}"))).ToList();

            stored.TryGetValue(definition.Id, out var existing);

            if (!Changes(existing, eventType, schemas, migrations))
            {
                continue;
            }

            logger.Registering(definition.Id, EventTypeGeneration.First, eventStore);

            writes.Add(new UpdateOneModel<EventType>(
                Builders<EventType>.Filter.Eq(_ => _.Id, definition.Id),
                BuildUpdate(eventType, schemas, migrations))
            {
                IsUpsert = true
            });
            mutated.Add(definition.Id);
        }

        if (writes.Count > 0)
        {
            await collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }).ConfigureAwait(false);
        }

        mutated.ForEach(Invalidate);

        return mutated;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions()
    {
        using var result = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        return (await result.ToListAsync()).Select(_ => _.ToDefinition());
    }

    /// <inheritdoc/>
    public async Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId)
    {
        if (_definitionsByType.TryGetValue(eventTypeId, out var cached))
        {
            return cached;
        }

        var filter = GetFilterForSpecificEventType(eventTypeId);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var eventType = await result.FirstOrDefaultAsync();
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

        return _definitionsByType.GetOrAdd(eventTypeId, eventType.ToDefinition());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds)
    {
        var ids = eventTypeIds.ToList();
        var missing = ids.Where(id => !_schemasByTypeAndGeneration.ContainsKey((id, EventTypeGeneration.First))).ToList();

        if (missing.Count > 0)
        {
            var filter = Builders<EventType>.Filter.In(et => et.Id, missing);
            using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
            foreach (var document in await result.ToListAsync())
            {
                _schemasByTypeAndGeneration.GetOrAdd((document.Id, EventTypeGeneration.First), document.ToKernel());
            }
        }

        return ids
            .Select(id => _schemasByTypeAndGeneration.TryGetValue((id, EventTypeGeneration.First), out var schema) ? schema : null)
            .Where(_ => _ is not null)
            .Select(_ => _!)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<Concepts.Events.EventType> eventTypes)
    {
        var eventTypesList = eventTypes.ToList();
        var missing = eventTypesList.Where(et => !_schemasByTypeAndGeneration.ContainsKey((et.Id, et.Generation))).ToList();

        if (missing.Count > 0)
        {
            var ids = missing.ConvertAll(et => et.Id);
            var filter = Builders<EventType>.Filter.In(et => et.Id, ids);
            using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
            var mongoTypeMap = (await result.ToListAsync()).ToDictionary(m => m.Id);
            foreach (var eventType in missing)
            {
                if (mongoTypeMap.TryGetValue(eventType.Id, out var document))
                {
                    _schemasByTypeAndGeneration.GetOrAdd((eventType.Id, eventType.Generation), document.ToKernel(eventType.Generation));
                }
            }
        }

        return eventTypesList
            .Select(et => _schemasByTypeAndGeneration.TryGetValue((et.Id, et.Generation), out var schema) ? schema : null)
            .Where(_ => _ is not null)
            .Select(_ => _!)
            .ToList();
    }

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId)
    {
        _definitionsByType.TryRemove(eventTypeId, out _);
        foreach (var key in _schemasByTypeAndGeneration.Keys)
        {
            if (key.Id == eventTypeId)
            {
                _schemasByTypeAndGeneration.TryRemove(key, out _);
            }
        }
    }

    static bool Changes(
        EventType? existing,
        EventTypeToRegister eventType,
        Dictionary<string, BsonDocument> schemas,
        List<EventTypeMigration> migrations)
    {
        if (existing is null)
        {
            return true;
        }

        if (existing.Owner != eventType.Definition.Owner ||
            existing.Source != eventType.Source ||
            existing.Tombstone != eventType.Definition.Tombstone)
        {
            return true;
        }

        if (schemas.Any(_ => !existing.Schemas.TryGetValue(_.Key, out var storedSchema) || storedSchema != _.Value))
        {
            return true;
        }

        return migrations.Count > 0 && !migrations.SequenceEqual(existing.Migrations ?? []);
    }

    static UpdateDefinition<EventType> BuildUpdate(
        EventTypeToRegister eventType,
        Dictionary<string, BsonDocument> schemas,
        List<EventTypeMigration> migrations)
    {
        var update = Builders<EventType>.Update
            .Set(_ => _.Owner, eventType.Definition.Owner)
            .Set(_ => _.Source, eventType.Source)
            .Set(_ => _.Tombstone, eventType.Definition.Tombstone);

        update = schemas.Aggregate(
            update,
            (current, schema) => current.Set($"{nameof(EventType.Schemas).ToCamelCase()}.{schema.Key}", schema.Value));

        if (migrations.Count > 0)
        {
            update = update.Set(_ => _.Migrations, migrations);
        }

        return update;
    }

    bool InvalidateIfMutated(EventTypeId eventTypeId, bool mutated)
    {
        if (mutated)
        {
            Invalidate(eventTypeId);
        }

        return mutated;
    }

    IMongoCollection<EventType> GetCollection() => sharedDatabase.GetCollection<EventType>(WellKnownCollectionNames.EventTypes);

    FilterDefinition<EventType> GetFilterForSpecificEventType(EventTypeId type) => Builders<EventType>.Filter.Eq(et => et.Id, type);
}
