// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Defines a manager of <see cref="IMongoClient"/> instances.
/// </summary>
public interface IMongoDBClientManager
{
    /// <summary>
    /// Get a <see cref="IMongoClient"/> for a specific <see cref="MongoClientSettings"/>.
    /// </summary>
    /// <param name="settings"><see cref="MongoClientSettings"/> to get for.</param>
    /// <returns><see cref="IMongoClient"/> instance.</returns>
    IMongoClient GetClientFor(MongoClientSettings settings);
}
