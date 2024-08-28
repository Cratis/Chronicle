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
    ConcurrentDictionary<EventTypeId, ConcurrentDictionary<EventGeneration, EventTypeSchema>> _schemasByTypeAndGeneration = [];

    /// <inheritdoc/>
    public async Task Populate()
    {
        logger.Populating(eventStore);

        var findResult = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        var allSchemas = findResult.ToList();

        _schemasByTypeAndGeneration = new(
            allSchemas!.GroupBy(_ => _.EventType)
            .ToDictionary(
                _ => (EventTypeId)_.Key,
                _ => new ConcurrentDictionary<EventGeneration, EventTypeSchema>(_.ToDictionary(es => (EventGeneration)es.Generation, es => es.ToEventSchema()))));
    }

    /// <inheritdoc/>
    public async Task Register(EventType type, string friendlyName, JsonSchema schema)
    {
        // If we have a schema for the event type on the given generation and the schemas differ - throw an exception (only in production)
        // .. if they're the same. Ignore saving.
        // If this is a new generation, there must be an upcaster and downcaster associated with the schema
        // .. do not allow generational gaps
        // if (await HasFor(type.Id, type.Generation)) return;
        var eventSchema = new EventTypeSchema(type, schema);
        if (!_schemasByTypeAndGeneration.TryGetValue(type.Id, out var value))
        {
            value = [];
            _schemasByTypeAndGeneration[type.Id] = value;
        }

        eventSchema.Schema.ResetFlattenedProperties();

        if (value.TryGetValue(type.Generation, out var existingSchema))
        {
            existingSchema.Schema.ResetFlattenedProperties();
            if (existingSchema.Schema.ToJson() == schema.ToJson())
            {
                return;
            }
        }

        schema.EnsureFlattenedProperties();
        logger.Registering(friendlyName, type.Id, type.Generation, eventStore);

        var mongoEventSchema = eventSchema.ToMongoDB();
        value[type.Generation] = eventSchema;
        await GetCollection().ReplaceOneAsync(
            _ => _.Id == mongoEventSchema.Id,
            mongoEventSchema,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes()
    {
        var result = await GetCollection().FindAsync(_ => true).ConfigureAwait(false);
        var schemas = result.ToList();
        return schemas
            .GroupBy(_ => _.EventType)
            .Select(_ => _.OrderByDescending(_ => _.Generation).First().ToEventSchema());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType)
    {
        var collection = GetCollection();
        var filter = Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, eventType.Id.Value);
        var result = await collection.FindAsync(filter).ConfigureAwait(false);
        var schemas = result.ToList();
        return schemas
            .OrderBy(_ => _.Generation)
            .Select(_ => _.ToEventSchema());
    }

    /// <inheritdoc/>
    public async Task<EventTypeSchema> GetFor(EventTypeId type, EventGeneration? generation = default)
    {
        generation ??= EventGeneration.First;
        if (_schemasByTypeAndGeneration.TryGetValue(type, out var generationalSchemas) && generationalSchemas.TryGetValue(generation, out var schema))
        {
            return schema;
        }

        var filter = GetFilterForSpecificSchema(type, generation);
        var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var schemas = result.ToList();
        _schemasByTypeAndGeneration[type] = new ConcurrentDictionary<EventGeneration, EventTypeSchema>(schemas.ToDictionary(_ => (EventGeneration)_.Generation, _ => _.ToEventSchema()));

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
    public async Task<bool> HasFor(EventTypeId type, EventGeneration? generation = default)
    {
        generation ??= EventGeneration.First;
        if (_schemasByTypeAndGeneration.TryGetValue(type, out var generationalSchemas) && generationalSchemas.ContainsKey(generation))
        {
            return true;
        }

        var filter = GetFilterForSpecificSchema(type, generation);
        var result = await GetCollection().FindAsync(filter).ConfigureAwait(false);
        var schemas = result.ToList();
        return schemas.Count == 1;
    }

    IMongoCollection<EventSchemaMongoDB> GetCollection() => sharedDatabase.GetCollection<EventSchemaMongoDB>(WellKnownCollectionNames.Schemas);

    FilterDefinition<EventSchemaMongoDB> GetFilterForSpecificSchema(EventTypeId type, EventGeneration? generation) => Builders<EventSchemaMongoDB>.Filter.And(
                   Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, type.Value),
                   Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.Generation, (generation ?? EventGeneration.First).Value));
}
