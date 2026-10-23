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
        var keysById = keys.GroupBy(converter.ToBsonValue).ToDictionary(group => group.Key, group => group.First());
        foreach (var page in keysById.Chunk(1000))
        {
            var missing = page.ToDictionary();
            using var cursor = await collection.FindAsync(Builders<BsonDocument>.Filter.In("_id", missing.Keys));
            while (await cursor.MoveNextAsync())
            {
                foreach (var document in cursor.Current)
                {
                    if (missing.Remove(document["_id"], out var key))
                    {
                        seed(key, document);
                    }
                }
            }

            foreach (var id in missing.Keys)
            {
                _knownMisses.TryAdd(id.ToString()!, 0);
            }
        }
    }
}
