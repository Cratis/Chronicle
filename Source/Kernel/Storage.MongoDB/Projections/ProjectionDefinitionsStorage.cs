// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Json;
using Cratis.Chronicle.Storage.Projections;
using MongoDB.Bson;
using MongoDB.Driver;
using ProjectionDefinition = Cratis.Chronicle.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

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
    public Task<bool> Has(ProjectionId id) =>
        Collection.Find(new BsonDocument("_id", id.Value)).AnyAsync();

    /// <inheritdoc/>
    public async Task<ProjectionDefinition> Get(ProjectionId id)
    {
        var result = await Collection.FindAsync(filter: new BsonDocument("_id", id.Value));
        var document = result.Single();
        return projectionSerializer.Deserialize(JsonNode.Parse(document.ToJson())!);
    }

    /// <inheritdoc/>
    public Task Delete(ProjectionId id) =>
        Collection.DeleteOneAsync(new BsonDocument("_id", id.Value));

    /// <inheritdoc/>
    public async Task Save(ProjectionDefinition definition)
    {
        var json = projectionSerializer.Serialize(definition);
        var document = BsonDocument.Parse(json.ToJsonString());
        document["_id"] = definition.Identifier.Value;

        await Collection.ReplaceOneAsync(
            filter: new BsonDocument("_id", definition.Identifier.Value),
            replacement: document,
            options: new ReplaceOptions { IsUpsert = true });
    }
}
