// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Defines the common database at the top level for all event stores.
/// </summary>
public interface IDatabase
{
    /// <summary>
    /// Gets a <see cref="IMongoCollection{T}"/> for a specific type of document.
    /// </summary>
    /// <param name="collectionName">Optional name of collection.</param>
    /// <typeparam name="T">Type of document.</typeparam>
    /// <returns><see cref="IMongoCollection{T}"/> instance.</returns>
    IMongoCollection<T> GetCollection<T>(string? collectionName = null);

    /// <summary>
    /// Get the <see cref="IEventStoreDatabase"/> for a specific <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to get for.</param>
    /// <returns>The <see cref="IEventStoreDatabase"/> instance.</returns>
    IEventStoreDatabase GetEventStoreDatabase(EventStoreName eventStore);

    /// <summary>
    /// Get the <see cref="IMongoDatabase"/> for a specific <see cref="EventStoreName"/> and <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to get for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to get for.</param>
    /// <returns>The <see cref="IMongoDatabase"/> instance.</returns>
    IMongoDatabase GetReadModelDatabase(EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
