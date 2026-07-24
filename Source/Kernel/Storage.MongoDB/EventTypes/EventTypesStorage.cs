// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <inheritdoc/>
    public async Task Register(Concepts.Events.EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code)
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

        await GetCollection().UpdateOneAsync(
            _ => _.Id == type.Id,
            update,
            new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);
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
        var filter = GetFilterForSpecificEventType(type);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var schemas = await result.ToListAsync();
        return schemas.Count == 1;
    }

    /// <inheritdoc/>
    public async Task Register(EventTypeDefinition definition)
    {
        logger.Registering(definition.Id, EventTypeGeneration.First, eventStore);

        var mongoEventType = definition.ToMongoDB();

        await GetCollection().ReplaceOneAsync(
            _ => _.Id == definition.Id,
            mongoEventType,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
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
        var filter = Builders<EventType>.Filter.In(et => et.Id, ids);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var schemas = await result.ToListAsync();
        return schemas.Select(_ => _.ToKernel());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<Concepts.Events.EventType> eventTypes)
    {
        var eventTypesList = eventTypes.ToList();
        var ids = eventTypesList.ConvertAll(et => et.Id);
        var filter = Builders<EventType>.Filter.In(et => et.Id, ids);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var mongoTypeMap = (await result.ToListAsync()).ToDictionary(m => m.Id);
        return eventTypesList
            .Where(et => mongoTypeMap.ContainsKey(et.Id))
            .Select(et => mongoTypeMap[et.Id].ToKernel(et.Generation));
    }

    IMongoCollection<EventType> GetCollection() => sharedDatabase.GetCollection<EventType>(WellKnownCollectionNames.EventTypes);

    FilterDefinition<EventType> GetFilterForSpecificEventType(EventTypeId type) => Builders<EventType>.Filter.Eq(et => et.Id, type);
}
