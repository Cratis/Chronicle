// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Keys;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Holds read-only, page-scoped absence knowledge independently of bulk write planning.
/// </summary>
/// <param name="converter">The converter for read-model keys.</param>
internal sealed class SinkPagePrefetch(IMongoDBConverter converter)
{
    readonly ConcurrentDictionary<string, byte> _knownMisses = new();

    /// <summary>
    /// Checks whether this page has established that a key is absent.
    /// </summary>
    /// <param name="cacheKey">The serialized key used by the bulk cache.</param>
    /// <returns>Whether the key is a known miss.</returns>
    public bool IsKnownMiss(string cacheKey) => _knownMisses.ContainsKey(cacheKey);

    /// <summary>
    /// Discards the current page's absence knowledge.
    /// </summary>
    public void Clear() => _knownMisses.Clear();

    /// <summary>
    /// Invalidates absence knowledge when a key is evicted after a write.
    /// </summary>
    /// <param name="cacheKey">The serialized key used by the bulk cache.</param>
    public void Forget(string cacheKey) => _knownMisses.TryRemove(cacheKey, out _);

    /// <summary>
    /// Reads distinct page keys in bounded queries, reporting hits and retaining misses.
    /// </summary>
    /// <param name="collection">The active read-model collection.</param>
    /// <param name="keys">The page keys.</param>
    /// <param name="seed">The callback for seeding an existing bulk cache without replacing pending changes.</param>
    /// <returns>A task representing the reads.</returns>
    public async Task Prefetch(IMongoCollection<BsonDocument> collection, IEnumerable<Key> keys, Action<Key, BsonDocument> seed)
    {
        Clear();
        var pageKeys = keys.ToArray();
        await PrefetchFlatKeys(collection, pageKeys.OfType<FlatCompositeKey>(), seed);
        var keysById = pageKeys.Where(key => key is not FlatCompositeKey)
            .GroupBy(key => SinkCacheKey.Identity(converter.ToBsonValue(key)))
            .ToDictionary(group => group.Key, group => group.First());
        foreach (var page in keysById.Chunk(1000))
        {
            await PrefetchLegacyBatch(collection, page, seed);
        }
    }

    async Task PrefetchLegacyBatch(IMongoCollection<BsonDocument> collection, KeyValuePair<BsonValue, Key>[] page, Action<Key, BsonDocument> seed)
    {
        var missing = page.ToDictionary();
        var allReturnedIdsMatched = true;
        using var cursor = await collection.FindAsync(Builders<BsonDocument>.Filter.In("_id", missing.Values.Select(converter.ToBsonValue)));
        while (await cursor.MoveNextAsync())
        {
            foreach (var document in cursor.Current)
            {
                if (missing.Remove(SinkCacheKey.Identity(document["_id"]), out var key))
                {
                    seed(key, document);
                }
                else
                {
                    allReturnedIdsMatched = false;
                }
            }
        }

        if (allReturnedIdsMatched)
        {
            foreach (var id in missing.Keys) _knownMisses.TryAdd(SinkCacheKey.ForIdentity(id), 0);
        }
    }

    async Task PrefetchFlatKeys(IMongoCollection<BsonDocument> collection, IEnumerable<FlatCompositeKey> keys, Action<Key, BsonDocument> seed)
    {
        foreach (var shape in keys.GroupBy(key => string.Join('\0', key.Parts.Keys.Order(StringComparer.Ordinal))))
        {
            var distinct = shape.GroupBy(key => SinkCacheKey.For(converter.ToBsonValue(key))).Select(group => group.First());
            foreach (var page in distinct.Chunk(1000))
            {
                await PrefetchFlatBatch(collection, page, seed);
            }
        }
    }

    async Task PrefetchFlatBatch(IMongoCollection<BsonDocument> collection, FlatCompositeKey[] page, Action<Key, BsonDocument> seed)
    {
        var missing = page.ToDictionary(key => SinkCacheKey.For(converter.ToBsonValue(key)));
        var allReturnedTuplesMatched = true;
        var filter = Builders<BsonDocument>.Filter.In("_id", page.Select(converter.ToBsonValue));
        using var cursor = await collection.FindAsync(filter);
        while (await cursor.MoveNextAsync())
        {
            foreach (var document in cursor.Current)
            {
                if (missing.Remove(SinkCacheKey.For(document["_id"]), out var key))
                {
                    seed(key, document);
                }
                else
                {
                    allReturnedTuplesMatched = false;
                }
            }
        }

        if (allReturnedTuplesMatched)
        {
            foreach (var cacheKey in missing.Keys) _knownMisses.TryAdd(cacheKey, 0);
        }
    }
}
