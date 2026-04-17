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
    ConcurrentBag<EventType> _eventTypes = new();

    /// <summary>
    /// Populates the event types storage with existing event types from the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Populate()
    {
        logger.Populating(eventStore);

        using var findResult = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        _eventTypes = new ConcurrentBag<EventType>(await findResult.ToListAsync());
    }

    /// <inheritdoc/>
    public async Task Register(Concepts.Events.EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code)
    {
        logger.Registering(type.Id, type.Generation, eventStore);

        var generationAsString = type.Generation.ToString();

        // Build the merged event type: preserve all existing schemas and add/update the current one
        var schemas = new Dictionary<string, BsonDocument>();
        var migrations = new List<EventTypeMigration>();

        var currentFromMemory = _eventTypes.FirstOrDefault(_ => _.Id == type.Id);
        if (currentFromMemory is not null)
        {
            foreach (var (key, value) in currentFromMemory.Schemas)
            {
                schemas[key] = value;
            }

            if (currentFromMemory.Migrations is not null)
            {
                migrations.AddRange(currentFromMemory.Migrations);
            }
        }

        schemas[generationAsString] = BsonDocument.Parse(schema.ToJson());

        var mongoEventType = new EventType(type.Id, owner, source, type.Tombstone, schemas, migrations);

        if (_eventTypes.Any(_ => _.Id == type.Id))
        {
            _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != type.Id));
        }

        _eventTypes.Add(mongoEventType);

        await GetCollection().ReplaceOneAsync(
            _ => _.Id == mongoEventType.Id,
            mongoEventType,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
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
        var generationAsString = generation.ToString();
        var cached = _eventTypes.FirstOrDefault(_ => _.Id == type && _.Schemas.ContainsKey(generationAsString));
        if (cached is not null)
        {
            return cached.ToKernel(generation);
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

        return schemas[0].ToKernel(generation);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        generation ??= EventTypeGeneration.First;
        var generationAsString = generation.ToString();

        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generationAsString)))
        {
            return true;
        }

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

        // Merge into existing event types in memory
        _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != definition.Id));
        _eventTypes.Add(mongoEventType);

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

    IMongoCollection<EventType> GetCollection() => sharedDatabase.GetCollection<EventType>(WellKnownCollectionNames.EventTypes);

    FilterDefinition<EventType> GetFilterForSpecificEventType(EventTypeId type) => Builders<EventType>.Filter.Eq(et => et.Id, type);
}
