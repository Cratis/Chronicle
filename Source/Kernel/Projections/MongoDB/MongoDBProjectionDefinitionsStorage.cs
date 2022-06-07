// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Json;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Events.Projections.MongoDB;

/// <summary>
/// Represents a <see cref="IProjectionDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
[SingletonPerMicroservice]
public class MongoDBProjectionDefinitionsStorage : IProjectionDefinitionsStorage
{
    readonly ISharedDatabase _sharedDatabase;
    readonly IJsonProjectionSerializer _projectionSerializer;
    IMongoCollection<BsonDocument> Collection => _sharedDatabase.GetCollection<BsonDocument>("projection-definitions");

    /// <summary>
    /// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
    /// </summary>
    /// <param name="sharedDatabase">The <see cref="ISharedDatabase"/>.</param>
    /// <param name="projectionSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
    public MongoDBProjectionDefinitionsStorage(ISharedDatabase sharedDatabase, IJsonProjectionSerializer projectionSerializer)
    {
        _sharedDatabase = sharedDatabase;
        _projectionSerializer = projectionSerializer;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        var result = await Collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
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

        await Collection.ReplaceOneAsync(
            filter: new BsonDocument("_id", id),
            options: new ReplaceOptions { IsUpsert = true },
            replacement: document);
    }
}
