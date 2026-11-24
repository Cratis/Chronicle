// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

public partial class Sink
{
    /// <inheritdoc/>
    public async Task<Option<Key>> TryFindRootKeyByChildValueInRoot(PropertyPath childPropertyPath, object childValue, Key rootKey)
    {
        await SettleGuardedBulkReads();
        if (!CanConvertScopedRootKey(rootKey)) return Option<Key>.None();

        if (_isBulkMode && TryAnswerFromBulk(rootKey, out var state))
        {
            return FindScopedRootInBulkState(childPropertyPath, childValue, rootKey, state);
        }

        var collection = Collection;
        var filter = Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(rootKey)) &
            Builders<BsonDocument>.Filter.Eq(childPropertyPath.ToMongoDB(), childValue.ToBsonValue());
        using var result = await collection.FindAsync(
            filter,
            new FindOptions<BsonDocument>
            {
                Projection = Builders<BsonDocument>.Projection.Include("_id"),
                Limit = 1
            });
        var document = await result.FirstOrDefaultAsync();
        if (document is null || !document.TryGetValue("_id", out var id)) return Option<Key>.None();
        return new Option<Key>(new Key(id.IsGuid ? id.AsGuid : id.ToString()!, ArrayIndexers.NoIndexers));
    }

    Option<Key> FindScopedRootInBulkState(PropertyPath childPropertyPath, object childValue, Key rootKey, ExpandoObject? state)
    {
        if (state is null || !TryFindValueInDocument(state, childPropertyPath.Segments.ToArray(), 0, childValue)) return Option<Key>.None();
        var cacheKey = SinkCacheKey.For(converter.ToBsonValue(rootKey));
        return new Option<Key>(_bulkKeysByCacheKey.GetValueOrDefault(cacheKey, rootKey));
    }

    bool CanConvertScopedRootKey(Key rootKey)
    {
        var schema = readModel.GetSchemaForLatestGeneration();
        var keyPropertyName = schema.HasKeyProperty() ? schema.GetKeyProperty().Name : schema.GetLikelyKeyPropertyName();
        if (string.IsNullOrEmpty(keyPropertyName)) return true;
        var format = schema.GetSchemaPropertyForPropertyPath(keyPropertyName)?.Format?.TrimEnd('?').ToLowerInvariant();
        return format is not ("guid" or "uuid") || Guid.TryParse(rootKey.Value?.ToString(), out _);
    }
}
