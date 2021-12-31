// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Cratis.MongoDB;
using MongoDB.Driver;
using NJsonSchema;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents an implementation of <see cref="ISchemaStore"/>.
    /// </summary>
    [Singleton]
    public class SchemaStore : ISchemaStore
    {
        const string SchemasCollection = "schemas";
        readonly IMongoCollection<EventSchemaMongoDB> _collection;
        Dictionary<EventTypeId, Dictionary<EventGeneration, EventSchema>> _schemasByTypeAndGeneration = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaStore"/> class.
        /// </summary>
        /// <param name="sharedDatabase">The <see cref="ISharedDatabase"/>.</param>
        public SchemaStore(ISharedDatabase sharedDatabase)
        {
            _collection = sharedDatabase.GetCollection<EventSchemaMongoDB>(SchemasCollection);
        }

        /// <inheritdoc/>
        public async Task Register(EventType type, string friendlyName, JsonSchema schema)
        {
            await PopulateIfNotPopulated();

            // If we have a schema for the event type on the given generation and the schemas differ - throw an exception (only in production)
            // .. if they're the same. Ignore saving.
            // If this is a new generation, there must be an upcaster and downcaster associated with the schema
            // .. do not allow generational gaps

            // if (await HasFor(type.Id, type.Generation)) return;

            schema.SetDisplayName(friendlyName);
            schema.SetGeneration(type.Generation);

            var eventSchema =  new EventSchema(type, schema).ToMongoDB();

            await _collection.ReplaceOneAsync(

                _ => _.Id == eventSchema.Id,
                eventSchema,
                new ReplaceOptions { IsUpsert = true}
            );
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<EventSchema>> GetLatestForAllEventTypes()
        {
            await PopulateIfNotPopulated();

            var result = await _collection.FindAsync(_ => true);
            var schemas = await result.ToListAsync();
            return schemas
                .GroupBy(_ => _.EventType)
                .Select(_ => _.OrderByDescending(_ => _.Generation).First().ToEventSchema());
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<EventSchema>> GetAllGenerationsForEventType(EventType eventType)
        {
            await PopulateIfNotPopulated();
            var filter = Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, eventType.Id.Value);
            var all = _collection.Find(_ => _.EventType == eventType.Id.Value).ToList();
            var result = await _collection.FindAsync(filter);
            var schemas = await result.ToListAsync();
            return schemas
                .OrderBy(_ => _.Generation)
                .Select(_ => _.ToEventSchema());
        }

        /// <inheritdoc/>
        public async Task<EventSchema> GetFor(EventTypeId type, EventGeneration? generation = default)
        {
            await PopulateIfNotPopulated();
            generation ??= EventGeneration.First;
            if (_schemasByTypeAndGeneration.ContainsKey(type) && _schemasByTypeAndGeneration[type].ContainsKey(generation))
            {
                return _schemasByTypeAndGeneration[type][generation];
            }

            var filter = GetFilterForSpecificSchema(type, generation);
            var result = await _collection.FindAsync(filter);
            var schemas = await result.ToListAsync();
            _schemasByTypeAndGeneration[type] = schemas.ToDictionary(_ => (EventGeneration)_.Generation, _ => _.ToEventSchema());

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
            var result = await _collection.FindAsync(filter);
            var schemas = await result.ToListAsync();
            return schemas.Count == 1;
        }

        FilterDefinition<EventSchemaMongoDB> GetFilterForSpecificSchema(EventTypeId type, EventGeneration? generation) => Builders<EventSchemaMongoDB>.Filter.And(
                       Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, type.Value),
                       Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.Generation, (generation ?? EventGeneration.First).Value)
                   );

        async Task PopulateIfNotPopulated()
        {
            if (_schemasByTypeAndGeneration.Count == 0)
            {
                await Populate();
            }
        }

        async Task Populate()
        {
            var findResult = await _collection.FindAsync(_ => true);
            var allSchemas = await findResult.ToListAsync();

            _schemasByTypeAndGeneration =
                allSchemas!.GroupBy(_ => _.EventType)
                .ToDictionary(
                    _ => (EventTypeId)_.Key,
                    _ => _.ToDictionary(es => (EventGeneration)es.Generation, es => es.ToEventSchema()));
        }
    }
}
