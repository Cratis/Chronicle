// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Extension methods for ensuring MongoDB indexes exist on a collection.
/// </summary>
public static class MongoDBIndexExtensions
{
    /// <summary>
    /// Ensures the given indexes exist on the collection at most once per collection for the lifetime of the given tracker.
    /// </summary>
    /// <param name="collection">The <see cref="IMongoCollection{T}"/> to ensure indexes on.</param>
    /// <param name="ensuredCollections">Tracker of collections whose indexes have already been ensured, keyed by full collection name.</param>
    /// <param name="models">The <see cref="CreateIndexModel{T}"/> instances to ensure. Each must carry a name.</param>
    /// <typeparam name="T">Type of document in the collection.</typeparam>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    /// <remarks>
    /// The tracker is only marked once creation succeeds, so a failed attempt is retried on the next call. A rare
    /// concurrent duplicate ensure is harmless — index creation for an already-existing index is a no-op.
    /// </remarks>
    public static async Task EnsureIndexesOnceAsync<T>(
        this IMongoCollection<T> collection,
        ConcurrentDictionary<string, byte> ensuredCollections,
        params CreateIndexModel<T>[] models)
    {
        if (ensuredCollections.ContainsKey(collection.CollectionNamespace.FullName))
        {
            return;
        }

        await collection.EnsureIndexesAsync(models).ConfigureAwait(false);
        ensuredCollections.TryAdd(collection.CollectionNamespace.FullName, 0);
    }

    /// <summary>
    /// Gets the set of existing index names for a collection.
    /// </summary>
    /// <param name="collection">The <see cref="IMongoCollection{T}"/> to get index names for.</param>
    /// <typeparam name="T">Type of document in the collection.</typeparam>
    /// <returns>The set of existing index names.</returns>
    public static async Task<IReadOnlySet<string>> GetIndexNamesAsync<T>(this IMongoCollection<T> collection)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        using var cursor = await collection.Indexes.ListAsync().ConfigureAwait(false);
        await cursor.ForEachAsync(index =>
        {
            if (index.TryGetValue("name", out var name))
            {
                names.Add(name.AsString);
            }
        }).ConfigureAwait(false);
        return names;
    }

    /// <summary>
    /// Ensures the given indexes exist on the collection, creating only the ones that are missing in a single operation.
    /// </summary>
    /// <param name="collection">The <see cref="IMongoCollection{T}"/> to ensure indexes on.</param>
    /// <param name="models">The <see cref="CreateIndexModel{T}"/> instances to ensure. Each must carry a name.</param>
    /// <typeparam name="T">Type of document in the collection.</typeparam>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    public static async Task EnsureIndexesAsync<T>(this IMongoCollection<T> collection, params CreateIndexModel<T>[] models)
    {
        var existing = await collection.GetIndexNamesAsync().ConfigureAwait(false);
        var missing = models.Where(model => model.Options?.Name is { } name && !existing.Contains(name)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        await collection.Indexes.CreateManyAsync(missing).ConfigureAwait(false);
    }
}
