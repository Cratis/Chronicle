// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
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
        if (_isBulkMode && TryAnswerFromBulk(rootKey, out var state))
        {
            return state is not null && TryFindValueInDocument(state, childPropertyPath.Segments.ToArray(), 0, childValue)
                ? new Option<Key>(rootKey)
                : Option<Key>.None();
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
}
