// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Defines the interface for a database providing access to various DbContext scopes.
/// </summary>
public interface IDatabase
{
    /// <summary>
    /// Gets a database context scope for the cluster.
    /// </summary>
    /// <returns>A <see cref="DbContextScope{ClusterDbContext}"/> for the cluster.</returns>
    Task<DbContextScope<ClusterDbContext>> Cluster();

    /// <summary>
    /// Gets a database context scope for the specified event store.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <returns>A <see cref="DbContextScope{EventStoreDbContext}"/> for the specified event store.</returns>
    Task<DbContextScope<EventStoreDbContext>> EventStore(EventStoreName eventStore);
}
