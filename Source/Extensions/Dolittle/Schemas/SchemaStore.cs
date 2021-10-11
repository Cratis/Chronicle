// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;
using Cratis.Reflection;
using Cratis.Strings;
using Cratis.Types;
using Dolittle.SDK.Artifacts;
using Dolittle.SDK.Events;
using MongoDB.Driver;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

namespace Cratis.Extensions.Dolittle.Schemas
{
    /// <summary>
    /// Represents an implementation of <see cref="ISchemaStore"/>.
    /// </summary>
    [Singleton]
    public class SchemaStore : ISchemaStore
    {
        const string DatabaseName = "schema_store";
        const string SchemasCollection = "schemas";
        static readonly JSchemaGenerator _generator;
        readonly IDictionary<Type, ICanExtendSchemaForType> _schemaInformationForTypesProviders;
        readonly IMongoDatabase _database;
        readonly IMongoCollection<EventSchemaMongoDB> _collection;
        readonly ITypes _types;
        Dictionary<EventTypeId, Dictionary<uint, EventSchema>> _schemasByTypeAndGeneration = new();

        static SchemaStore()
        {
            _generator = new JSchemaGenerator
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            _generator.GenerationProviders.Add(new StringEnumGenerationProvider());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaStore"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="SchemaStoreConfiguration"/>.</param>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for creating MongoDB clients.</param>
        /// <param name="schemaInformationForTypesProviders"><see cref="IInstancesOf{T}"/> of <see cref="ICanExtendSchemaForType"/>.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        public SchemaStore(
            SchemaStoreConfiguration configuration,
            IMongoDBClientFactory mongoDBClientFactory,
            IInstancesOf<ICanExtendSchemaForType> schemaInformationForTypesProviders,
            ITypes types)
        {
            _schemaInformationForTypesProviders = schemaInformationForTypesProviders.ToDictionary(_ => _.Type, _ => _);
            var mongoUrlBuilder = new MongoUrlBuilder
            {
                Servers = new[] { new MongoServerAddress(configuration.Host, configuration.Port) }
            };
            var url = mongoUrlBuilder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);
            var client = mongoDBClientFactory.Create(settings);
            _database = client.GetDatabase(DatabaseName);
            _collection = _database.GetCollection<EventSchemaMongoDB>(SchemasCollection);
            _types = types;
        }

        /// <inheritdoc/>
        public async Task DiscoverGenerateAndConsolidate()
        {
            foreach (var eventType in _types.All.Where(_ => _.HasAttribute<EventTypeAttribute>()))
            {
                var schema = GenerateFor(eventType);
                await Save(schema);
            }

            var findResult = await _collection.FindAsync(_ => true);
            var allSchemas = findResult.ToList();

            _schemasByTypeAndGeneration = allSchemas.GroupBy(_ => _.EventType)
                .ToDictionary(
                    _ => (EventTypeId)_.Key,
                    _ => _.ToDictionary(es => es.Generation, es => es.ToEventSchema()));
        }

        /// <inheritdoc/>
        public EventSchema GenerateFor(Type type)
        {
            TypeIsMissingEventType.ThrowIfMissingEventType(type);
            var eventTypeAttribute = type.GetCustomAttribute<EventTypeAttribute>()!;

            var typeSchema = _generator.Generate(type);
            typeSchema.SetDisplayName(type.Name);
            typeSchema.SetGeneration(eventTypeAttribute.EventType.Generation.Value);

            var eventSchema = new EventSchema(eventTypeAttribute.EventType, typeSchema);
            ExtendSchema(type, eventSchema, typeSchema);

            return eventSchema;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<EventSchema>> GetLatestForAllEventTypes()
        {
            var result = await _collection.FindAsync(_ => true);
            var schemas = await result.ToListAsync();
            return schemas
                .GroupBy(_ => _.EventType)
                .Select(_ => _.OrderByDescending(_ => _.Generation).First().ToEventSchema());
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<EventSchema>> GetAllGenerationsForEventType(global::Dolittle.SDK.Events.EventType eventType)
        {
            var filter = Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, eventType.Id.Value);
            var all = _collection.Find(_ => _.EventType == eventType.Id.Value).ToList();
            var result = await _collection.FindAsync(filter);
            var schemas = await result.ToListAsync();
            return schemas
                .OrderBy(_ => _.Generation)
                .Select(_ => _.ToEventSchema());
        }

        /// <inheritdoc/>
        public async Task<EventSchema> GetFor(EventTypeId type, Generation? generation = default)
        {
            generation ??= Generation.First;
            if (_schemasByTypeAndGeneration.ContainsKey(type) && _schemasByTypeAndGeneration[type].ContainsKey(generation))
            {
                return _schemasByTypeAndGeneration[type][generation];
            }

            var filter = GetFilterForSpecificSchema(type, generation);
            var result = await _collection.FindAsync(filter);
            var schemas = await result.ToListAsync();
            _schemasByTypeAndGeneration[type] = schemas.ToDictionary(_ => _.Generation, _ => _.ToEventSchema());

            return schemas[0].ToEventSchema();
        }

        /// <inheritdoc/>
        public async Task<bool> HasFor(EventTypeId type, Generation? generation = default)
        {
            generation ??= Generation.First;
            if (_schemasByTypeAndGeneration.ContainsKey(type) && _schemasByTypeAndGeneration[type].ContainsKey(generation))
            {
                return true;
            }

            var filter = GetFilterForSpecificSchema(type, generation);
            var result = await _collection.FindAsync(filter);
            var schemas = await result.ToListAsync();
            return schemas.Count == 1;
        }

        /// <inheritdoc/>
        public async Task Save(EventSchema eventSchema)
        {
            // If we have a schema for the event type on the given generation and the schemas differ - throw an exception
            // .. if they're the same. Ignore saving.
            // If this is a new generation, there must be an upcaster and downcaster associated with the schema
            // .. do not allow generational gaps

            if (await HasFor(eventSchema.EventType.Id, eventSchema.EventType.Generation)) return;
            await _collection.InsertOneAsync(eventSchema.ToMongoDB()).ConfigureAwait(false);
        }

        void ExtendSchema(Type type, EventSchema eventSchema, JSchema typeSchema)
        {
            foreach (var provider in _schemaInformationForTypesProviders.Where(_ => _.Key == type).Select(_ => _.Value))
            {
                provider.Extend(eventSchema, typeSchema);

                foreach ((var property, var propertySchema) in eventSchema.Schema.Properties)
                {
                    var propertyName = property.ToPascalCase();
                    var propertyInfo = type.GetProperty(propertyName);
                    if (propertyInfo != null)
                    {
                        ExtendSchema(propertyInfo.PropertyType, eventSchema, propertySchema);
                    }
                }
            }
        }

        FilterDefinition<EventSchemaMongoDB> GetFilterForSpecificSchema(EventTypeId type, Generation? generation) => Builders<EventSchemaMongoDB>.Filter.And(
                       Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.EventType, type.Value),
                       Builders<EventSchemaMongoDB>.Filter.Eq(_ => _.Generation, (generation ?? Generation.First).Value)
                   );
    }
}
