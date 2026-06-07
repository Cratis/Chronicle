// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

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

    /// <summary>
    /// Gets a database context scope for a specific read model table within a namespace.
    /// The table's column shape is derived from <paramref name="columns"/>; the migration runs
    /// the first time the table is accessed for a given combination of name and column set.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The name of the namespace.</param>
    /// <param name="containerName">The container name of the read model (table name).</param>
    /// <param name="columns">The columns derived from the read model's <see cref="Schemas.JsonSchema"/>.</param>
    /// <returns>A <see cref="DbContextScope{ReadModelDbContext}"/> for the specified read model table.</returns>
    Task<DbContextScope<ReadModelDbContext>> ReadModelTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string containerName, IReadOnlyList<ProjectedColumn> columns);

    /// <summary>
    /// Clears all cached table-migration state that was recorded for the given connection string prefix.
    /// Call this after deleting or recreating SQLite database files so that the next access
    /// re-creates every table from scratch instead of returning an early-out from the static cache.
    /// </summary>
    /// <param name="connectionStringPrefix">
    /// Connection string prefix to match (e.g. the base file path).
    /// All cache entries whose key starts with this prefix are evicted.
    /// Pass an empty string to evict every entry.
    /// </param>
    void ClearTableMigrationCache(string connectionStringPrefix);

#if DEVELOPMENT
    /// <summary>
    /// Atomically wipes every database owned by this <see cref="IDatabase"/> instance and
    /// invalidates the per-context migration cache. This member is only compiled when the
    /// <c>DEVELOPMENT</c> preprocessor symbol is defined — production builds of
    /// <c>Storage.Sql</c> do not expose it.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// For SQLite, this closes the pooled connections and drops every table in every database
    /// file whose name shares the cluster file's basename — relying on SQLite's own RESERVED /
    /// EXCLUSIVE lock to serialize against concurrent grain activations rather than racing on
    /// <c>File.Delete</c>. For PostgreSQL and Microsoft SQL Server, this truncates every user
    /// table that is not part of the preserved identity set across the cluster database and
    /// every sibling event-store / namespace / read-model database. The wipe and the
    /// migration-cache invalidation are performed together so that no subsequent caller can
    /// observe a state where the cache says a table is migrated but the underlying file or
    /// row does not exist.
    /// </remarks>
    Task Wipe();
#endif
}
