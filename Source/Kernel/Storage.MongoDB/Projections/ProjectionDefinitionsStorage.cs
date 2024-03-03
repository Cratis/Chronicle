// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Kernel.Storage.Projections;
using Cratis.Projections.Definitions;
using Cratis.Projections.Json;
using Aksio.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB.Projections;

/// <summary>
/// Represents a <see cref="IProjectionDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
public class ProjectionDefinitionsStorage : IProjectionDefinitionsStorage
{
    readonly IEventStoreDatabase _eventStoreDatabase;
    readonly IJsonProjectionSerializer _projectionSerializer;

    /// <summary>
    /// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
    /// </summary>
    /// <param name="sharedDatabase">The <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="projectionSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
    public ProjectionDefinitionsStorage(
        IEventStoreDatabase sharedDatabase,
        IJsonProjectionSerializer projectionSerializer)
    {
        _eventStoreDatabase = sharedDatabase;
        _projectionSerializer = projectionSerializer;
    }

    IMongoCollection<BsonDocument> Collection => _eventStoreDatabase.GetCollection<BsonDocument>("projection-definitions");

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        var result = await Collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
        var definitionsAsBson = result.ToList();
        return definitionsAsBson.Select(_ =>
        {
            _.Remove("_id");
            var definitionAsJson = _.ToJson();
            return _projectionSerializer.Deserialize(JsonNode.Parse(definitionAsJson)!);
        }).ToArray();
    }

    /// <inheritdoc/>
    public async Task Save(ProjectionDefinition definition)
    {
        var json = _projectionSerializer.Serialize(definition);
        var document = BsonDocument.Parse(json.ToJsonString());
        var id = new BsonBinaryData(definition.Identifier.Value, GuidRepresentation.Standard);
        document["_id"] = id;

        await Collection.ReplaceOneAsync(
            filter: new BsonDocument("_id", id),
            replacement: document,
            options: new ReplaceOptions { IsUpsert = true });
    }
}
