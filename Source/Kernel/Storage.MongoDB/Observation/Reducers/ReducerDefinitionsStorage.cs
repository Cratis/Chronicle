// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Observation.Reducers.Json;
using Cratis.Chronicle.Storage.Observation.Reducers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Reducers;

/// <summary>
/// Represents a <see cref="IReducerDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
/// </remarks>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
/// <param name="reducerDefinitionSerializer">Serializer for <see cref="ReducerDefinition"/>.</param>
public class ReducerDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase,
    IJsonReducerDefinitionSerializer reducerDefinitionSerializer) : IReducerDefinitionsStorage
{
    IMongoCollection<BsonDocument> Collection => eventStoreDatabase.GetCollection<BsonDocument>(WellKnownCollectionNames.ReducerDefinitions);

    /// <inheritdoc/>
    public async Task<IEnumerable<ReducerDefinition>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
        var definitionsAsBson = result.ToList();
        return definitionsAsBson.Select(_ =>
        {
            _.Remove("_id");
            var definitionAsJson = _.ToJson();
            return reducerDefinitionSerializer.Deserialize(JsonNode.Parse(definitionAsJson)!);
        }).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ReducerId id) =>
        Collection.Find(new BsonDocument("_id", id.Value)).AnyAsync();

    /// <inheritdoc/>
    public async Task<ReducerDefinition> Get(ReducerId id)
    {
        using var result = await Collection.FindAsync(filter: new BsonDocument("_id", id.Value));
        var document = result.Single();
        return reducerDefinitionSerializer.Deserialize(JsonNode.Parse(document.ToJson())!);
    }

    /// <inheritdoc/>
    public Task Delete(ReducerId id) =>
        Collection.DeleteOneAsync(new BsonDocument("_id", id.Value));

    /// <inheritdoc/>
    public async Task Save(ReducerDefinition definition)
    {
        var json = reducerDefinitionSerializer.Serialize(definition);
        var document = BsonDocument.Parse(json.ToJsonString());
        document["_id"] = definition.Identifier.Value;

        await Collection.ReplaceOneAsync(
            filter: new BsonDocument("_id", definition.Identifier.Value),
            replacement: document,
            options: new ReplaceOptions { IsUpsert = true });
    }
}
