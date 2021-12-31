// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.MongoDB
{
    /// <summary>
    /// Defines the common database for event store that goes across all tenants.
    /// </summary>
    public interface ISharedDatabase
    {
        /// <summary>
        /// Gets a <see cref="IMongoCollection{T}"/> for a specific type of document.
        /// </summary>
        /// <param name="collectionName">Optional name of collection.</param>
        /// <typeparam name="T">Type of document.</typeparam>
        /// <returns><see cref="IMongoCollection{T}"/> instance.</returns>
        IMongoCollection<T> GetCollection<T>(string? collectionName = null);
    }
}
