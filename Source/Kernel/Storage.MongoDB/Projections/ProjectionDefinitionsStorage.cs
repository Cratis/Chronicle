// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
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
/// <param name="projectionSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
public class ProjectionDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase,
    IJsonProjectionSerializer projectionSerializer) : IProjectionDefinitionsStorage
{
    IMongoCollection<BsonDocument> Collection => eventStoreDatabase.GetCollection<BsonDocument>("projection-definitions");

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        var result = await Collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
        var definitionsAsBson = result.ToList();
        return definitionsAsBson.Select(_ =>
        {
            _.Remove("_id");
            var definitionAsJson = _.ToJson();
            return projectionSerializer.Deserialize(JsonNode.Parse(definitionAsJson)!);
        }).ToArray();
    }

    /// <inheritdoc/>
    public async Task Save(ProjectionDefinition definition)
    {
        var json = projectionSerializer.Serialize(definition);
        var document = BsonDocument.Parse(json.ToJsonString());
        var id = new BsonBinaryData(definition.Identifier.Value, GuidRepresentation.Standard);
        document["_id"] = id;

        await Collection.ReplaceOneAsync(
            filter: new BsonDocument("_id", id),
            replacement: document,
            options: new ReplaceOptions { IsUpsert = true });
    }
}
