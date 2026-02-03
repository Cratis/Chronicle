// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Reactive;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;

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

    /// <inheritdoc/>
    public async Task Populate()
    {
        logger.Populating(eventStore);

        using var findResult = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        _eventTypes = new ConcurrentBag<EventType>(findResult.ToList());
    }

    /// <inheritdoc/>
    public async Task Register(Concepts.Events.EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code)
    {
        logger.Registering(type.Id, type.Generation, eventStore);

        // If we have a schema for the event type on the given generation and the schemas differ - throw an exception (only in production)
        // .. if they're the same. Ignore saving.
        // If this is a new generation, there must be an upcaster and downcaster associated with the schema
        // .. do not allow generational gaps
        // if (await HasFor(type.Id, type.Generation)) return;
        var generationAsString = type.Generation.ToString();
        var existingEventType = _eventTypes
            .FirstOrDefault(_ => _.Id == type.Id && _.Schemas.ContainsKey(generationAsString));

        if (existingEventType is not null)
        {
            var existingSchema = await JsonSchema.FromJsonAsync(existingEventType.Schemas[generationAsString].ToJson());
            if (existingSchema.ToJson() == schema.ToJson())
            {
                return;
            }
        }

        var eventSchema = new EventTypeSchema(type, owner, source, schema);
        if (_eventTypes.Any(_ => _.Id == type.Id))
        {
            _eventTypes = new ConcurrentBag<EventType>(_eventTypes.Where(_ => _.Id != type.Id));
        }
        _eventTypes.Add(eventSchema.ToMongoDB());

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
        var schemas = result.ToList();
        return schemas.Select(_ => _.ToKernel());
    }

    /// <inheritdoc/>
    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        generation ??= EventTypeGeneration.First;
        var generationAsString = generation.ToString();
        if (_eventTypes.Any(_ => _.Id == type && _.Schemas.ContainsKey(generationAsString)))
        {
            return _eventTypes.First(_ => _.Id == type && _.Schemas.ContainsKey(generationAsString)).ToKernel();
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

        return schemas[0].ToKernel();
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
        var schemas = result.ToList();
        return schemas.Count == 1;
    }

    IMongoCollection<EventType> GetCollection() => sharedDatabase.GetCollection<EventType>(WellKnownCollectionNames.EventTypes);

    FilterDefinition<EventType> GetFilterForSpecificEventType(EventTypeId type) => Builders<EventType>.Filter.Eq(et => et.Id, type);
}
