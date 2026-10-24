// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

public partial class Sink
{
    /// <inheritdoc/>
    public Task<CompositeKeyLookupResult> FindByCompositeKey(Key key)
    {
        if (converter.ToBsonValue(key) is not BsonDocument id) return Task.FromResult(CompositeKeyLookupResult.NotFound);
        var permutations = CompositeKeyPermutations.For(id);
        var filter = permutations.Count > 0
            ? Builders<BsonDocument>.Filter.In("_id", permutations)
            : CompositeKeyFieldFilter(id);
        if (permutations.Count == 0) logger.CompositeKeyCollectionScan(readModel.ContainerName, id.ElementCount);
        return WithPrimaryRetry(async () =>
        {
            var documents = await ReadPrimary(filter, 2);
            return documents.Count switch
            {
                0 => CompositeKeyLookupResult.NotFound,
                1 => CompositeKeyLookupResult.Found(expandoObjectConverter.ToExpandoObject(documents[0], readModel.GetSchemaForLatestGeneration())),
                _ => CompositeKeyLookupResult.AmbiguousLegacyKey
            };
        });
    }

    static FilterDefinition<BsonDocument> CompositeKeyFieldFilter(BsonDocument id)
    {
        var filter = Builders<BsonDocument>.Filter.Type("_id", BsonType.Document);
        foreach (var element in id)
        {
            filter &= Builders<BsonDocument>.Filter.Eq($"_id.{element.Name}", element.Value);
        }

        // A type predicate does not guarantee expression evaluation order for legacy scalar ids.
        var objectId = new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", "$_id"), "object" }),
            "$_id",
            new BsonDocument()
        });
        return filter & new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray
        {
            new BsonDocument("$size", new BsonDocument("$objectToArray", objectId)), id.ElementCount
        }));
    }
}
