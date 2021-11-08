// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Definitions;
using Cratis.Events.Projections.Json;
using Cratis.Extensions.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IProjectionDefinitions"/> for projection definitions in MongoDB.
    /// </summary>
    public class MongoDBProjectionDefinitions : IProjectionDefinitions
    {
        readonly JsonProjectionSerializer _projectionSerializer;
        readonly IMongoCollection<BsonDocument> _collection;

        /// <summary>
        /// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
        /// </summary>
        /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for connecting to mongo.</param>
        /// <param name="projectionSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
        public MongoDBProjectionDefinitions(IMongoDBClientFactory clientFactory, JsonProjectionSerializer projectionSerializer)
        {
            var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");

            _projectionSerializer = projectionSerializer;

            var client = clientFactory.Create(settings.Freeze());
            var database = client.GetDatabase("projections");
            _collection = database.GetCollection<BsonDocument>("definitions");
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ProjectionDefinition>> GetAll()
        {
            return Task.FromResult(Array.Empty<ProjectionDefinition>().AsEnumerable());
        }

        /// <inheritdoc/>
        public async Task Save(ProjectionDefinition definition)
        {
            var json = _projectionSerializer.Serialize(definition);
            var document = BsonDocument.Parse(json);
            var id =  BsonBinaryData.Create(definition.Identifier.Value);
            document["_id"] = id;

            await _collection.ReplaceOneAsync(
                filter: new BsonDocument("_id", id),
                options: new ReplaceOptions { IsUpsert = true },
                replacement: document
            );
        }
    }
}
