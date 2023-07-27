// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.MongoDB;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.Cratis.MongoDB;
using Aksio.Cratis.Schemas;
using MongoDB.Driver;
using NJsonSchema;

namespace Aksio.Cratis.Events.Schemas.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="ISchemaStore"/>.
/// </summary>
[SingletonPerMicroservice]
public class MongoDBSchemaStore : ISchemaStore
{
    readonly ISharedDatabase _sharedDatabase;
    Dictionary<EventTypeId, Dictionary<EventGeneration, EventSchema>> _schemasByTypeAndGeneration = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBSchemaStore"/> class.
    /// </summary>
    /// <param name="sharedDatabase">The <see cref="ISharedDatabase"/>.</param>
    public MongoDBSchemaStore(ISharedDatabase sharedDatabase)
    {
        _sharedDatabase = sharedDatabase;
    }

    /// <inheritdoc/>
    public Task Populate()
    {
        var findResult = GetCollection().Find(_ => true);
        var allSchemas = findResult.ToList();

        _schemasByTypeAndGeneration =
            allSchemas!.GroupBy(_ => _.EventType)
            .ToDictionary(
                _ => (EventTypeId)_.Key,
                _ => _.ToDictionary(es => (EventGeneration)es.Generation, es => es.ToEventSchema()));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Register(EventType type, string friendlyName, JsonSchema schema)
    {
        // If we have a schema for the event type on the given generation and the schemas differ - throw an exception (only in production)
        // .. if they're the same. Ignore saving.
        // If this is a new generation, there must be an upcaster and downcaster associated with the schema
        // .. do not allow generational gaps
        // if (await HasFor(type.Id, type.Generation)) return;
        schema.SetIsPublic(type.IsPublic);
        schema.SetDisplayName(friendlyName);
        schema.SetGeneration(type.Generation);

        var eventSchema = new EventSchema(type, schema);
        if (!_schemasByTypeAndGeneration.ContainsKey(type.Id))
        {
            _schemasByTypeAndGeneration[type.Id] = new();
        }
        eventSchema.Schema.ResetFlattenedProperties();
        var mongoEventSchema = eventSchema.ToMongoDB();
        schema.EnsureFlattenedProperties();
        _schemasByTypeAndGeneration[type.Id][type.Generation] = eventSchema;
        GetCollection().ReplaceOne(
            _ => _.Id == mongoEventSchema.Id,
            mongoEventSchema,
            new ReplaceOptions { IsUpsert = true });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventSchema>> GetLatestForAllEventTypes()
    {
        var result = GetCollection().Find(_ => true);
        var schemas = result.ToList();
        return Task.FromResult(schemas
            .GroupBy(_ => _.EventType)
            .Select(_ => _.OrderByDescending(_ => _.Generation).First().ToEventSchema()));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventSchema>> GetAllGenerationsForEventType(EventType eventType)
    {
        var collection = GetCollection();
        var filter = Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, eventType.Id.Value);
        var result = collection.Find(filter);
        var schemas = result.ToList();
        return Task.FromResult(schemas
            .OrderBy(_ => _.Generation)
            .Select(_ => _.ToEventSchema()));
    }

    /// <inheritdoc/>
    public Task<EventSchema> GetFor(EventTypeId type, EventGeneration? generation = default)
    {
        generation ??= EventGeneration.First;
        if (_schemasByTypeAndGeneration.TryGetValue(type, out var generationalSchemas) && generationalSchemas.TryGetValue(generation, out var schema))
        {
            return Task.FromResult(schema);
        }

        var filter = GetFilterForSpecificSchema(type, generation);
        var result = GetCollection().Find(filter);
        var schemas = result.ToList();
        _schemasByTypeAndGeneration[type] = schemas.ToDictionary(_ => (EventGeneration)_.Generation, _ => _.ToEventSchema());

        if (schemas.Count == 0)
        {
            throw new MissingEventSchemaForEventType(type, generation);
        }

        return Task.FromResult(schemas[0].ToEventSchema());
    }

    /// <inheritdoc/>
    public Task<bool> HasFor(EventTypeId type, EventGeneration? generation = default)
    {
        generation ??= EventGeneration.First;
        if (_schemasByTypeAndGeneration.TryGetValue(type, out var generationalSchemas) && generationalSchemas.ContainsKey(generation))
        {
            return Task.FromResult(true);
        }

        var filter = GetFilterForSpecificSchema(type, generation);
        var result = GetCollection().Find(filter);
        var schemas = result.ToList();
        return Task.FromResult(schemas.Count == 1);
    }

    IMongoCollection<EventSchemaMongoDB> GetCollection() => _sharedDatabase.GetCollection<EventSchemaMongoDB>(CollectionNames.Schemas);

    FilterDefinition<EventSchemaMongoDB> GetFilterForSpecificSchema(EventTypeId type, EventGeneration? generation) => Builders<EventSchemaMongoDB>.Filter.And(
                   Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, type.Value),
                   Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.Generation, (generation ?? EventGeneration.First).Value));
}
