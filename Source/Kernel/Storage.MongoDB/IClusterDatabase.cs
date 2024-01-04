// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Defines the common database for the Cratis cluster that goes across all microservices.
/// </summary>
public interface IClusterDatabase
{
    /// <summary>
    /// Gets a <see cref="IMongoCollection{T}"/> for a specific type of document.
    /// </summary>
    /// <param name="collectionName">Optional name of collection.</param>
    /// <typeparam name="T">Type of document.</typeparam>
    /// <returns><see cref="IMongoCollection{T}"/> instance.</returns>
    IMongoCollection<T> GetCollection<T>(string? collectionName = null);
}
