// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Reactors.Json;
using Cratis.Chronicle.Storage.Observation.Reactors;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Reactors;

/// <summary>
/// Represents a <see cref="IReactorDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
/// </remarks>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
/// <param name="reducerDefinitionSerializer">Serializer for <see cref="ReactorDefinition"/>.</param>
public class ReactorDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase,
    IJsonReactorDefinitionSerializer reducerDefinitionSerializer) : IReactorDefinitionsStorage
{
    IMongoCollection<BsonDocument> Collection => eventStoreDatabase.GetCollection<BsonDocument>(WellKnownCollectionNames.ReactorDefinitions);

    /// <inheritdoc/>
    public async Task<IEnumerable<ReactorDefinition>> GetAll()
    {
        var result = await Collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
        var definitionsAsBson = result.ToList();
        return definitionsAsBson.Select(_ =>
        {
            _.Remove("_id");
            var definitionAsJson = _.ToJson();
            return reducerDefinitionSerializer.Deserialize(JsonNode.Parse(definitionAsJson)!);
        }).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ReactorId id) =>
        Collection.Find(new BsonDocument("_id", id.Value)).AnyAsync();

    /// <inheritdoc/>
    public async Task<ReactorDefinition> Get(ReactorId id)
    {
        var result = await Collection.FindAsync(filter: new BsonDocument("_id", id.Value));
        var document = result.Single();
        return reducerDefinitionSerializer.Deserialize(JsonNode.Parse(document.ToJson())!);
    }

    /// <inheritdoc/>
    public Task Delete(ReactorId id) =>
        Collection.DeleteOneAsync(new BsonDocument("_id", id.Value));

    /// <inheritdoc/>
    public async Task Save(ReactorDefinition definition)
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
