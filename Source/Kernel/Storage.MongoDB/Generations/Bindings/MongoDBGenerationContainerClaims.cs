// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;

/// <summary>Finds claims in the same database as the physical collections.</summary>
/// <param name="readModelDb">The read-model database.</param>
public sealed class MongoDBGenerationContainerClaims(IMongoDatabase readModelDb) : IGenerationContainerClaims
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<GenerationContainerClaim>> For(string collection)
    {
        var bindings = await readModelDb.GetCollection<BsonDocument>(MaterializationBindingCollections.Bindings)
            .Find(Builders<BsonDocument>.Filter.Eq("generations.destination", collection)).ToListAsync();
        return bindings.SelectMany(_ => _["generations"].AsBsonArray).Select(_ => _.AsBsonDocument)
            .Where(_ => _["destination"].AsString == collection)
            .Select(_ => new GenerationContainerClaim(new(collection), new(checked((uint)_["generation"].AsInt32)), (MaterializationGenerationState)_["state"].AsInt32)).ToArray();
    }
}
