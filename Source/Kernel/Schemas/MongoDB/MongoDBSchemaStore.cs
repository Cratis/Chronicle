// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;
using NJsonSchema;

namespace Aksio.Cratis.Events.Schemas.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="ISchemaStore"/>.
/// </summary>
[SingletonPerMicroservice]
public class MongoDBSchemaStore : ISchemaStore
{
    const string SchemasCollection = "schemas";
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
    public async Task Populate()
    {
        var findResult = await GetCollection().FindAsync(_ => true);
        var allSchemas = await findResult.ToListAsync();

        _schemasByTypeAndGeneration =
            allSchemas!.GroupBy(_ => _.EventType)
            .ToDictionary(
                _ => (EventTypeId)_.Key,
                _ => _.ToDictionary(es => (EventGeneration)es.Generation, es => es.ToEventSchema()));
    }

    /// <inheritdoc/>
    public async Task Register(EventType type, string friendlyName, JsonSchema schema)
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
        _schemasByTypeAndGeneration[type.Id][type.Generation] = eventSchema;
        var mongoEventSchema = eventSchema.ToMongoDB();
        await GetCollection().ReplaceOneAsync(
            _ => _.Id == mongoEventSchema.Id,
            mongoEventSchema,
            new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventSchema>> GetLatestForAllEventTypes()
    {
        var result = await GetCollection().FindAsync(_ => true);
        var schemas = await result.ToListAsync();
        return schemas
            .GroupBy(_ => _.EventType)
            .Select(_ => _.OrderByDescending(_ => _.Generation).First().ToEventSchema());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventSchema>> GetAllGenerationsForEventType(EventType eventType)
    {
        var collection = GetCollection();
        var filter = Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, eventType.Id.Value);
        var result = await collection.FindAsync(filter);
        var schemas = await result.ToListAsync();
        return schemas
            .OrderBy(_ => _.Generation)
            .Select(_ => _.ToEventSchema());
    }

    /// <inheritdoc/>
    public async Task<EventSchema> GetFor(EventTypeId type, EventGeneration? generation = default)
    {
        generation ??= EventGeneration.First;
        if (_schemasByTypeAndGeneration.ContainsKey(type) && _schemasByTypeAndGeneration[type].ContainsKey(generation))
        {
            return _schemasByTypeAndGeneration[type][generation];
        }

        var filter = GetFilterForSpecificSchema(type, generation);
        var result = await GetCollection().FindAsync(filter);
        var schemas = await result.ToListAsync();
        _schemasByTypeAndGeneration[type] = schemas.ToDictionary(_ => (EventGeneration)_.Generation, _ => _.ToEventSchema());

        if (schemas.Count == 0)
        {
            throw new MissingEventSchemaForEventType(type, generation);
        }

        return schemas[0].ToEventSchema();
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventTypeId type, EventGeneration? generation = default)
    {
        generation ??= EventGeneration.First;
        if (_schemasByTypeAndGeneration.ContainsKey(type) && _schemasByTypeAndGeneration[type].ContainsKey(generation))
        {
            return true;
        }

        var filter = GetFilterForSpecificSchema(type, generation);
        var result = await GetCollection().FindAsync(filter);
        var schemas = await result.ToListAsync();
        return schemas.Count == 1;
    }

    IMongoCollection<EventSchemaMongoDB> GetCollection() => _sharedDatabase.GetCollection<EventSchemaMongoDB>(SchemasCollection);

    FilterDefinition<EventSchemaMongoDB> GetFilterForSpecificSchema(EventTypeId type, EventGeneration? generation) => Builders<EventSchemaMongoDB>.Filter.And(
                   Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, type.Value),
                   Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.Generation, (generation ?? EventGeneration.First).Value));
}
