// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

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

    /// <summary>
    /// Gets a database context scope for the specified event store namespace.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The name of the namespace.</param>
    /// <returns>A <see cref="DbContextScope{NamespaceDbContext}"/> for the specified event store namespace.</returns>
    Task<DbContextScope<NamespaceDbContext>> Namespace(EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Gets a database context scope for a specific unique constraint table within a namespace.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The name of the namespace.</param>
    /// <param name="constraintName">The name of the constraint.</param>
    /// <returns>A <see cref="DbContextScope{UniqueConstraintDbContext}"/> for the specified constraint table.</returns>
    Task<DbContextScope<UniqueConstraintDbContext>> UniqueConstraintTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string constraintName);

    /// <summary>
    /// Gets a database context scope for a specific event sequence table within a namespace.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The name of the namespace.</param>
    /// <param name="eventSequenceName">The name of the event sequence.</param>
    /// <returns>A <see cref="DbContextScope{EventSequenceDbContext}"/> for the specified event sequence table.</returns>
    Task<DbContextScope<EventSequenceDbContext>> EventSequenceTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string eventSequenceName);
}
