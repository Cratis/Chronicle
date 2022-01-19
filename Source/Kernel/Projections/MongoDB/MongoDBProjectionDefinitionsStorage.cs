// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Json;
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IProjectionDefinitionsStorage"/> for projection definitions in MongoDB.
    /// </summary>
    public class MongoDBProjectionDefinitionsStorage : IProjectionDefinitionsStorage
    {
        readonly IJsonProjectionSerializer _projectionSerializer;
        readonly IMongoCollection<BsonDocument> _collection;

        /// <summary>
        /// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
        /// </summary>
        /// <param name="sharedDatabase">The <see cref="ISharedDatabase"/>.</param>
        /// <param name="projectionSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
        public MongoDBProjectionDefinitionsStorage(ISharedDatabase sharedDatabase, IJsonProjectionSerializer projectionSerializer)
        {
            _projectionSerializer = projectionSerializer;
            _collection = sharedDatabase.GetCollection<BsonDocument>("projection-definitions");
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProjectionDefinition>> GetAll()
        {
            var result = await _collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
            var definitionsAsBson = result.ToList();
            return definitionsAsBson.Select(_ =>
            {
                _.Remove("_id");
                var definitionAsJson = _.ToJson();
                return _projectionSerializer.Deserialize(definitionAsJson);
            }).ToArray();
        }

        /// <inheritdoc/>
        public async Task Save(ProjectionDefinition definition)
        {
            var json = _projectionSerializer.Serialize(definition);
            var document = BsonDocument.Parse(json);
            var id = new BsonBinaryData(definition.Identifier.Value, GuidRepresentation.Standard);
            document["_id"] = id;

            await _collection.ReplaceOneAsync(
                filter: new BsonDocument("_id", id),
                options: new ReplaceOptions { IsUpsert = true },
                replacement: document);
        }
    }
}
