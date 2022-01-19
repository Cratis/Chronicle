// Copyright (c) Cratis. All rights reserved.
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
    public class MongoDBProjectionPipelineDefinitionsStorage : IProjectionPipelineDefinitionsStorage
    {
        readonly IJsonProjectionPipelineSerializer _projectionPipelineSerializer;
        readonly IMongoCollection<BsonDocument> _collection;

        /// <summary>
        /// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
        /// </summary>
        /// <param name="sharedDatabase">The <see cref="ISharedDatabase"/>.</param>
        /// <param name="projectionPipelineSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
        public MongoDBProjectionPipelineDefinitionsStorage(
            ISharedDatabase sharedDatabase,
            IJsonProjectionPipelineSerializer projectionPipelineSerializer)
        {
            _projectionPipelineSerializer = projectionPipelineSerializer;
            _collection = sharedDatabase.GetCollection<BsonDocument>("projection-pipelines");
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProjectionPipelineDefinition>> GetAll()
        {
            var result = await _collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
            var definitionsAsBson = result.ToList();
            return definitionsAsBson.Select(_ =>
            {
                _.Remove("_id");
                var definitionAsJson = _.ToJson();
                return _projectionPipelineSerializer.Deserialize(definitionAsJson);
            }).ToArray();
        }

        /// <inheritdoc/>
        public async Task Save(ProjectionPipelineDefinition definition)
        {
            var json = _projectionPipelineSerializer.Serialize(definition);
            var document = BsonDocument.Parse(json);
            var id = new BsonBinaryData(definition.ProjectionId.Value, GuidRepresentation.Standard);
            document["_id"] = id;

            await _collection.ReplaceOneAsync(
                filter: new BsonDocument("_id", id),
                options: new ReplaceOptions { IsUpsert = true },
                replacement: document);
        }
    }
}
