// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Defines a system that connects to the MongoDB databases of the kernel, through the configured connection resolution.
/// </summary>
public interface IMongoDBStorageConnections
{
    /// <summary>
    /// Get a database.
    /// </summary>
    /// <param name="kind">The <see cref="StorageConnectionKind"/>.</param>
    /// <param name="derivedName">The name Chronicle derives for the database, used unless a database is resolved.</param>
    /// <param name="eventStore">The event store, empty for the cluster.</param>
    /// <param name="namespace">The namespace, empty for the cluster and an event store.</param>
    /// <returns>The <see cref="IMongoDatabase"/>.</returns>
    IMongoDatabase GetDatabase(StorageConnectionKind kind, string derivedName, string eventStore = "", string @namespace = "");
}
