// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NJsonSchema;

namespace Cratis.Events.MongoDB.EventTypes;

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

    /// <inheritdoc/>
    public async Task Populate()
    {
        logger.Populating(eventStore);

        using var findResult = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        _eventTypes = new ConcurrentBag<EventType>(findResult.ToList());
    }

    /// <inheritdoc/>
    public async Task Register(Chronicle.Concepts.Events.EventType type, JsonSchema schema)
    {
        // If we have a schema for the event type on the given generation and the schemas differ - throw an exception (only in production)
        // .. if they're the same. Ignore saving.
        // If this is a new generation, there must be an upcaster and downcaster associated with the schema
        // .. do not allow generational gaps
        // if (await HasFor(type.Id, type.Generation)) return;
        var existingEventType = _eventTypes
            .FirstOrDefault(_ => _.Id == type.Id && _.Schemas.ContainsKey(type.Generation));

        if (existingEventType is not null)
        {
            var existingSchema = await JsonSchema.FromJsonAsync(existingEventType.Schemas[type.Generation]);
            existingSchema.ResetFlattenedProperties();
            if (existingSchema.ToJson() == schema.ToJson())
            {
                return;
            }
        }

        var eventSchema = new EventTypeSchema(type, schema);
        if (_eventTypes.Any(_ => _.Id == type.Id))
        {
            _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != type.Id));
        }
        _eventTypes.Add(eventSchema.ToMongoDB());
        eventSchema.Schema.ResetFlattenedProperties();

        schema.EnsureFlattenedProperties();
        logger.Registering(type.Id, type.Generation, eventStore);

        var mongoEventSchema = eventSchema.ToMongoDB();
        await GetCollection().ReplaceOneAsync(
            _ => _.Id == mongoEventSchema.Id,
            mongoEventSchema,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes()
    {
        using var result = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        var schemas = result.ToList();
        return schemas.Select(_ => _.ToEventSchema());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(Chronicle.Concepts.Events.EventType eventType)
    {
        var collection = GetCollection();
        var filter = GetFilterForSpecificEventType(eventType.Id);
        using var result = await collection.FindAsync(filter).ConfigureAwait(false);
        var schemas = result.ToList();
        return schemas.Select(_ => _.ToEventSchema());
    }

    /// <inheritdoc/>
    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        generation ??= EventTypeGeneration.First;
        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generation)))
        {
            return _eventTypes.First(_ => _.Id == type && _.Schemas.ContainsKey(generation)).ToEventSchema();
        }

        var filter = GetFilterForSpecificEventType(type);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var schemas = result.ToList();

        if (schemas.Count == 0)
        {
            throw new MissingEventSchemaForEventType(
                eventStore,
                type,
                generation);
        }

        return schemas[0].ToEventSchema();
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        generation ??= EventTypeGeneration.First;

        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generation)))
        {
            return true;
        }

        var filter = GetFilterForSpecificEventType(type);
        using var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var schemas = result.ToList();
        return schemas.Count == 1;
    }

    IMongoCollection<EventType> GetCollection() => sharedDatabase.GetCollection<EventType>(WellKnownCollectionNames.EventTypes);

    FilterDefinition<EventType> GetFilterForSpecificEventType(EventTypeId type) => Builders<EventType>.Filter.Eq(et => et.Id, type);
}
