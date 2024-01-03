// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Persistence.Projections;
using Aksio.Cratis.MongoDB;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Projections.Json;
using Aksio.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Projections;

/// <summary>
/// Represents a <see cref="IProjectionDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
[SingletonPerMicroservice]
public class ProjectionPipelineDefinitionsStorage : IProjectionPipelineDefinitionsStorage
{
    readonly IJsonProjectionPipelineSerializer _projectionPipelineSerializer;
    readonly IMongoCollection<BsonDocument> _collection;

    /// <summary>
    /// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
    /// </summary>
    /// <param name="sharedDatabase">The <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="projectionPipelineSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
    public ProjectionPipelineDefinitionsStorage(
        IEventStoreDatabase sharedDatabase,
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
            replacement: document,
            options: new ReplaceOptions { IsUpsert = true });
    }
}
