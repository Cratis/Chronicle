// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.MongoDB;
using Cratis.Kernel.Storage.Projections;
using Cratis.Projections.Definitions;
using Cratis.Projections.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB.Projections;

/// <summary>
/// Represents a <see cref="IProjectionDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
/// </remarks>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
/// <param name="projectionPipelineSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
public class ProjectionPipelineDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase,
    IJsonProjectionPipelineSerializer projectionPipelineSerializer) : IProjectionPipelineDefinitionsStorage
{
    readonly IMongoCollection<BsonDocument> _collection = eventStoreDatabase.GetCollection<BsonDocument>("projection-pipelines");

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionPipelineDefinition>> GetAll()
    {
        var result = await _collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
        var definitionsAsBson = result.ToList();
        return definitionsAsBson.Select(_ =>
        {
            _.Remove("_id");
            var definitionAsJson = _.ToJson();
            return projectionPipelineSerializer.Deserialize(definitionAsJson);
        }).ToArray();
    }

    /// <inheritdoc/>
    public async Task Save(ProjectionPipelineDefinition definition)
    {
        var json = projectionPipelineSerializer.Serialize(definition);
        var document = BsonDocument.Parse(json);
        var id = new BsonBinaryData(definition.ProjectionId.Value, GuidRepresentation.Standard);
        document["_id"] = id;

        await _collection.ReplaceOneAsync(
            filter: new BsonDocument("_id", id),
            replacement: document,
            options: new ReplaceOptions { IsUpsert = true });
    }
}
