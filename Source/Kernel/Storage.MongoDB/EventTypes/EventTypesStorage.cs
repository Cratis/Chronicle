// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json.Nodes;
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
public partial class EventTypesStorage(
    EventStoreName eventStore,
    IEventStoreDatabase sharedDatabase,
    ILogger<EventTypesStorage> logger) : IEventTypesStorage
{
    /// <inheritdoc/>
    public async Task<bool> Register(Concepts.Events.EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code, EventTypeVisibility visibility = EventTypeVisibility.Unspecified, string origin = "")
    {
        logger.Registering(type.Id, type.Generation, eventStore);

        var generationKey = type.Generation.ToString();
        var schemaDocument = BsonDocument.Parse(schema.ToJson());
        using var cursor = await GetCollection().FindAsync(_ => _.Id == type.Id).ConfigureAwait(false);
        var existing = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        visibility = EffectiveVisibility(existing, visibility);
        if (existing is not null &&
            existing.Owner == owner && existing.Source == source && existing.Tombstone == type.Tombstone &&
            existing.Visibility == visibility && (existing.Origin ?? string.Empty) == origin &&
            existing.Schemas.TryGetValue(generationKey, out var storedSchema) &&
            JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(storedSchema.ToJson(), schemaDocument.ToJson()))
        {
            return false;
        }

        // Merge the incoming generation into the stored document by setting only its schema entry and the
        // metadata fields. Other generations - which another silo may have registered - are left untouched,
        // so a rolling deploy never clobbers generations this silo does not know about.
        var update = Builders<EventType>.Update
            .Set(_ => _.Owner, owner)
            .Set(_ => _.Source, source)
            .Set(_ => _.Tombstone, type.Tombstone)
            .Set(_ => _.Visibility, visibility)
            .Set(_ => _.Origin, origin)
            .Set($"{nameof(EventType.Schemas).ToCamelCase()}.{generationKey}", schemaDocument);

        var result = await GetCollection().UpdateOneAsync(
            _ => _.Id == type.Id,
            update,
            new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);

        return result.ModifiedCount > 0 || result.UpsertedId is not null;
    }

    /// <inheritdoc/>
    public async Task<bool> ReplaceSchema(EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema schema)
    {
        var path = $"{nameof(EventType.Schemas).ToCamelCase()}.{generation}";
        var filter = Builders<EventType>.Filter.Eq(_ => _.Id, eventTypeId) & Builders<EventType>.Filter.Exists(path);
        var update = Builders<EventType>.Update.Set(path, BsonDocument.Parse(schema.ToJson()));
        var result = await GetCollection().UpdateOneAsync(filter, update).ConfigureAwait(false);
        return result.ModifiedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes()
    {
        using var result = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        var schemas = await result.ToListAsync();
        return schemas.ConvertAll(LatestFor);
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() =>
        new TransformingSubject<IEnumerable<EventType>, IEnumerable<EventTypeSchema>>(
            GetCollection().Observe(),
            _ => _.Select(LatestFor).ToArray());

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(Concepts.Events.EventType eventType)
    {
        var collection = GetCollection();
        var filter = GetFilterForSpecificEventType(eventType.Id);
        using var result = await collection.FindAsync(filter).ConfigureAwait(false);
        var schemas = await result.ToListAsync();
        return schemas.SelectMany(_ => _.ToKernelForAllGenerations()).ToList();
    }

    /// <inheritdoc/>
    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        generation ??= EventTypeGeneration.First;

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

        return schemas[0].ToKernel(generation);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        generation ??= EventTypeGeneration.First;

        var filter = GetFilterForSpecificEventType(type);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var eventTypes = await result.ToListAsync();

        return eventTypes.Count > 0 && eventTypes[0].Schemas.ContainsKey(generation.ToString());
    }

    /// <inheritdoc/>
    public async Task<bool> Register(EventTypeDefinition definition)
    {
        logger.Registering(definition.Id, EventTypeGeneration.First, eventStore);

        // The full definition carries no visibility, so what is already stored is kept.
        using var existingCursor = await GetCollection().FindAsync(_ => _.Id == definition.Id).ConfigureAwait(false);
        var existingDocument = await existingCursor.FirstOrDefaultAsync().ConfigureAwait(false);
        var mongoEventType = Merge(existingDocument, definition.ToMongoDB(
            visibility: existingDocument?.Visibility ?? EventTypeVisibility.Unspecified,
            origin: existingDocument?.Origin ?? string.Empty));
        if (!Changes(existingDocument, mongoEventType))
        {
            return false;
        }

        // Target only the current definition; immutable versions may have been recorded since the read.
        var result = await GetCollection().UpdateOneAsync(
            _ => _.Id == definition.Id,
            BuildUpdate(existingDocument, mongoEventType),
            new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);

        return result.ModifiedCount > 0 || result.UpsertedId is not null;
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

        var prepared = new Dictionary<EventTypeId, EventType>();
        foreach (var eventType in all)
        {
            var definition = eventType.Definition;
            var previous = prepared.GetValueOrDefault(definition.Id) ?? stored.GetValueOrDefault(definition.Id);
            prepared[definition.Id] = Merge(previous, definition.ToMongoDB(
                owner: definition.Owner,
                source: eventType.Source,
                visibility: eventType.Visibility,
                origin: eventType.Origin));
        }

        var writes = new List<WriteModel<EventType>>();
        var mutated = new List<EventTypeId>();
        foreach (var (id, effective) in prepared)
        {
            stored.TryGetValue(id, out var existing);
            if (!Changes(existing, effective))
            {
                continue;
            }

            logger.Registering(id, EventTypeGeneration.First, eventStore);
            writes.Add(new UpdateOneModel<EventType>(
                Builders<EventType>.Filter.Eq(_ => _.Id, id),
                BuildUpdate(existing, effective))
            {
                IsUpsert = true
            });
            mutated.Add(id);
        }

        if (writes.Count > 0)
        {
            await collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }).ConfigureAwait(false);
        }

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

        return eventType.ToDefinition();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds)
    {
        var ids = eventTypeIds.ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var filter = Builders<EventType>.Filter.In(et => et.Id, ids);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var documents = (await result.ToListAsync()).ToDictionary(_ => _.Id);

        return ids
            .Where(documents.ContainsKey)
            .Select(id => documents[id].ToKernel(EventTypeGeneration.First))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<Concepts.Events.EventType> eventTypes)
    {
        var eventTypesList = eventTypes.ToList();
        if (eventTypesList.Count == 0)
        {
            return [];
        }

        var ids = eventTypesList.ConvertAll(et => et.Id);
        var filter = Builders<EventType>.Filter.In(et => et.Id, ids);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var documents = (await result.ToListAsync()).ToDictionary(_ => _.Id);

        return eventTypesList
            .Where(et => documents.ContainsKey(et.Id))
            .Select(et => documents[et.Id].ToKernel(et.Generation))
            .ToList();
    }

    static EventType Merge(EventType? existing, EventType incoming) => incoming with
    {
        Schemas = MergeSchemas(existing?.Schemas ?? new Dictionary<string, BsonDocument>(), incoming.Schemas),
        Visibility = EffectiveVisibility(existing, incoming.Visibility),
        Migrations = MergeMigrations(existing?.Migrations ?? [], incoming.Migrations ?? [])
    };

    static Dictionary<string, BsonDocument> MergeSchemas(IDictionary<string, BsonDocument> stored, IDictionary<string, BsonDocument> incoming)
    {
        var schemas = new Dictionary<string, BsonDocument>(stored);
        foreach (var (generation, schema) in incoming.Where(_ => !stored.TryGetValue(_.Key, out var previous) ||
            !JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(previous.ToJson(), _.Value.ToJson())))
        {
            schemas[generation] = schema;
        }
        return schemas;
    }

    static IEnumerable<EventTypeMigration> MergeMigrations(IEnumerable<EventTypeMigration> stored, IEnumerable<EventTypeMigration> incoming)
    {
        var migrations = stored.ToDictionary(_ => (_.FromGeneration, _.ToGeneration));
        foreach (var migration in incoming)
        {
            migrations[(migration.FromGeneration, migration.ToGeneration)] = migration;
        }
        var effective = migrations.Values.OrderBy(_ => _.FromGeneration.Value).ThenBy(_ => _.ToGeneration.Value).ToList();
        return MigrationsEqual(stored, effective) ? stored : effective;
    }

    static bool MigrationsEqual(IEnumerable<EventTypeMigration> stored, IEnumerable<EventTypeMigration> incoming)
    {
        var byEndpoint = stored.ToDictionary(_ => (_.FromGeneration, _.ToGeneration));
        var supplied = incoming.ToList();
        if (byEndpoint.Count != supplied.Count)
        {
            return false;
        }
        return supplied.TrueForAll(migration => byEndpoint.TryGetValue((migration.FromGeneration, migration.ToGeneration), out var previous) &&
            JsonNode.DeepEquals(JsonNode.Parse(previous.UpcastJmesPath.ToJson()), JsonNode.Parse(migration.UpcastJmesPath.ToJson())) &&
            JsonNode.DeepEquals(JsonNode.Parse(previous.DowncastJmesPath.ToJson()), JsonNode.Parse(migration.DowncastJmesPath.ToJson())));
    }

    static bool Changes(EventType? existing, EventType effective)
    {
        if (existing is null)
        {
            return true;
        }

        if (existing.Owner != effective.Owner ||
            existing.Source != effective.Source ||
            existing.Visibility != effective.Visibility ||
            (existing.Origin ?? string.Empty) != effective.Origin ||
            existing.Tombstone != effective.Tombstone)
        {
            return true;
        }

        if (effective.Schemas.Any(_ => !existing.Schemas.TryGetValue(_.Key, out var storedSchema) ||
            !JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(storedSchema.ToJson(), _.Value.ToJson())))
        {
            return true;
        }

        return !MigrationsEqual(existing.Migrations ?? [], effective.Migrations ?? []);
    }

    static EventTypeVisibility EffectiveVisibility(EventType? existing, EventTypeVisibility incoming) =>
        incoming == EventTypeVisibility.Unspecified && existing is not null ? existing.Visibility : incoming;

    static UpdateDefinition<EventType> BuildUpdate(EventType? existing, EventType effective)
    {
        var update = Builders<EventType>.Update
            .Set(_ => _.Owner, effective.Owner)
            .Set(_ => _.Source, effective.Source)
            .Set(_ => _.Visibility, effective.Visibility)
            .Set(_ => _.Origin, effective.Origin)
            .Set(_ => _.Tombstone, effective.Tombstone);

        update = effective.Schemas.Where(schema => existing is null ||
                !existing.Schemas.TryGetValue(schema.Key, out var stored) ||
                !JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(stored.ToJson(), schema.Value.ToJson()))
            .Aggregate(update, (current, schema) => current.Set($"{nameof(EventType.Schemas).ToCamelCase()}.{schema.Key}", schema.Value));

        if (!MigrationsEqual(existing?.Migrations ?? [], effective.Migrations ?? []))
        {
            update = update.Set(_ => _.Migrations, effective.Migrations);
        }

        return update;
    }

    IMongoCollection<EventType> GetCollection() => sharedDatabase.GetCollection<EventType>(WellKnownCollectionNames.EventTypes);

    FilterDefinition<EventType> GetFilterForSpecificEventType(EventTypeId type) => Builders<EventType>.Filter.Eq(et => et.Id, type);
}
