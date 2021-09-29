// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Extensions.MongoDB;
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
    public class SchemaStore : ISchemaStore
    {
        const string DatabaseName = "schema_store";
        const string SchemasCollection = "schemas";
        static readonly JSchemaGenerator _generator;
        readonly IDictionary<Type, ICanExtendSchemaForType> _schemaInformationForTypesProviders;
        readonly IMongoDatabase _database;
        readonly IMongoCollection<EventSchemaMongoDB> _collection;

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
        public SchemaStore(
            SchemaStoreConfiguration configuration,
            IMongoDBClientFactory mongoDBClientFactory,
            IInstancesOf<ICanExtendSchemaForType> schemaInformationForTypesProviders)
        {
            _schemaInformationForTypesProviders = schemaInformationForTypesProviders.ToDictionary(_ => _.Type, _ => _);
            var mongoUrlBuilder = new MongoUrlBuilder
            {
                Servers = new[] { new MongoServerAddress(configuration.Host, configuration.Port) }
            };
            var url = mongoUrlBuilder.ToMongoUrl();
            var client = mongoDBClientFactory.Create(url);
            _database = client.GetDatabase(DatabaseName);
            _collection = _database.GetCollection<EventSchemaMongoDB>(SchemasCollection);
        }

        /// <inheritdoc/>
        public EventSchema GenerateFor(Type type)
        {
            TypeIsMissingEventType.ThrowIfMissingEventType(type);
            var eventTypeAttribute = type.GetCustomAttribute<EventTypeAttribute>()!;

            var typeSchema = _generator.Generate(type);
            var eventSchema = new EventSchema(eventTypeAttribute.EventType, typeSchema);
            ExtendSchema(type, eventSchema, typeSchema);

            return eventSchema;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<EventSchema>> GetAll()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<EventSchema> GetFor(EventType type, Generation? generation = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<bool> HasFor(EventType type, Generation? generation = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public async Task Save(EventSchema eventSchema)
        {
            // If we have a schema for the event type on the given generation and the schemas differ - throw an exception
            // .. if they're the same. Ignore saving.
            // If this is a new generation, there must be an upcaster and downcaster associated with the schema
            // .. do not allow generational gaps
            var schemaToSave = new EventSchemaMongoDB
            {
                EventType = eventSchema.EventType.Id,
                Generation = eventSchema.EventType.Generation,
                Schema = eventSchema.Schema.ToString()
            };

            await _collection.InsertOneAsync(schemaToSave).ConfigureAwait(false);
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
    }
}
