// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Data.Common;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Resolves and caches permanent database bindings after proving candidate ownership.
/// </summary>
/// <param name="connectionString">The configured cluster connection.</param>
/// <param name="prepareConnection">Optional callback preparing the connections the catalog opens itself, for instance with a resolved identity.</param>
internal sealed class DatabaseNameResolver(string connectionString, Func<DbConnection, Task>? prepareConnection = null)
{
    readonly SqlDatabaseCatalog _catalog = new(connectionString, prepareConnection);
    readonly ConcurrentDictionary<DatabaseIdentity, string> _bindings = new();

    /// <summary>
    /// Rejects a configured cluster name the provider would truncate or refuse.
    /// </summary>
    /// <exception cref="DatabaseNameBindingFailure">The name exceeds the provider limit.</exception>
    internal void ValidateClusterName()
    {
        if (!DatabaseName.Fits(_catalog.ClusterName, _catalog.Provider))
        {
            throw new DatabaseNameBindingFailure($"Configured cluster database '{_catalog.ClusterName}' exceeds the {_catalog.Provider} limit of {DatabaseName.Limit(_catalog.Provider)} {(_catalog.Provider == Cratis.Arc.EntityFrameworkCore.DatabaseType.PostgreSql ? "UTF-8 bytes" : "UTF-16 code units")}. Configure a shorter cluster database name; it is never rewritten.");
        }
    }

    /// <summary>
    /// Clears only the memory cache, leaving permanent bindings intact.
    /// </summary>
    internal void Clear() => _bindings.Clear();

    /// <summary>
    /// Reserves the configured cluster database against derived-name collisions.
    /// </summary>
    /// <param name="context">The migrated cluster context.</param>
    /// <returns>The reservation operation.</returns>
    /// <exception cref="DatabaseNameBindingFailure">The reserved name disagrees with configuration.</exception>
    internal async Task ReserveCluster(ClusterDbContext context)
    {
        if (_bindings.ContainsKey(DatabaseIdentity.Cluster))
        {
            return;
        }

        var existing = await Query(context, DatabaseIdentity.Cluster).SingleOrDefaultAsync();
        if (existing is not null)
        {
            if (!await _catalog.Equal(existing.PhysicalName, _catalog.ClusterName))
            {
                throw new DatabaseNameBindingFailure("The configured cluster database does not match its reserved DatabaseNames binding.");
            }

            _bindings.TryAdd(DatabaseIdentity.Cluster, existing.PhysicalName);
            return;
        }

        await Insert(context, DatabaseIdentity.Cluster, _catalog.ClusterName, hashed: true);
    }

    /// <summary>
    /// Reloads one binding synchronously for the Orleans jobs options callback.
    /// </summary>
    /// <param name="identity">The namespace identity.</param>
    /// <param name="createCluster">Creates a cluster context without asynchronous migration.</param>
    /// <returns>The bound connection string.</returns>
    /// <exception cref="DatabaseNameBindingFailure">The identity is not bound.</exception>
    internal string Known(DatabaseIdentity identity, Func<ClusterDbContext> createCluster)
    {
        if (_bindings.TryGetValue(identity, out var known))
        {
            return _catalog.ConnectionString(known);
        }

        using var cluster = createCluster();
        var binding = Query(cluster, identity).SingleOrDefault() ?? throw new DatabaseNameBindingFailure($"DatabaseNames has no binding for {identity}. Create the namespace through NamespaceStorage.Ensure before resolving jobs storage.");

        _bindings.TryAdd(identity, binding.PhysicalName);
        return _catalog.ConnectionString(binding.PhysicalName);
    }

    /// <summary>
    /// Resolves prerequisites, proves ownership and persists a single immutable binding.
    /// </summary>
    /// <param name="identity">The requested identity.</param>
    /// <param name="cluster">Opens the migrated cluster.</param>
    /// <param name="eventStore">Creates an unmigrated context for metadata inspection.</param>
    /// <param name="namespace">Creates an unmigrated context for schema inspection.</param>
    /// <param name="logger">Logs safe separation of empty shared databases.</param>
    /// <returns>The bound physical connection string.</returns>
    internal async Task<string> Resolve(
        DatabaseIdentity identity,
        Func<Task<DbContextScope<ClusterDbContext>>> cluster,
        Func<string, EventStoreDbContext> eventStore,
        Func<string, NamespaceDbContext> @namespace,
        ILogger<DatabaseNameResolver> logger)
    {
        if (_bindings.TryGetValue(identity, out var known))
        {
            return _catalog.ConnectionString(known);
        }

        await using var scope = await cluster();
        var context = scope.DbContext;
        var binding = await Query(context, identity).SingleOrDefaultAsync();
        if (binding is not null)
        {
            _bindings.TryAdd(identity, binding.PhysicalName);
            return _catalog.ConnectionString(binding.PhysicalName);
        }

        if (identity.Kind != DatabaseKind.EventStore)
        {
            await Resolve(DatabaseIdentity.Store(identity.EventStore), cluster, eventStore, @namespace, logger);
        }

        if (identity.Kind == DatabaseKind.ReadModels && identity.Namespace != EventStoreNamespaceName.Default.Value)
        {
            await Resolve(identity with { Namespace = EventStoreNamespaceName.Default.Value }, cluster, eventStore, @namespace, logger);
        }

        var logicalName = identity.LogicalName(_catalog.ClusterName);
        var fits = DatabaseName.Fits(logicalName, _catalog.Provider);
        var legacy = DatabaseName.Clip(logicalName, _catalog.Provider, DatabaseName.Limit(_catalog.Provider));
        var candidate = logicalName;
        if (!fits)
        {
            candidate = _catalog.Provider == Cratis.Arc.EntityFrameworkCore.DatabaseType.PostgreSql && await _catalog.Exists(legacy)
                ? legacy
                : DatabaseName.Hashed(logicalName, _catalog.Provider);
        }
        var hashed = !fits && candidate != legacy;
        var ownership = new DatabaseOwnership(_catalog, eventStore, @namespace);
        if (!hashed)
        {
            var claimants = await ownership.Claimants(context, identity, candidate);
            var other = await context.DatabaseNames.AsNoTracking().SingleOrDefaultAsync(row => row.PhysicalName == candidate);
            if (other is not null && other.Identity != identity)
            {
                claimants.Add(other.Identity);
            }

            if (claimants.Any(claimant => claimant != identity && !identity.Outranks(claimant)))
            {
                candidate = await Separate(identity, candidate, claimants, ownership, logger);
                hashed = true;
            }
        }

        var physical = await Insert(context, identity, candidate, hashed);
        if (physical is null)
        {
            var claimants = await ownership.Claimants(context, identity, candidate);
            var owner = await context.DatabaseNames.AsNoTracking().SingleAsync(row => row.PhysicalName == candidate);
            claimants.Add(owner.Identity);
            candidate = await Separate(identity, candidate, claimants, ownership, logger);
            physical = await Insert(context, identity, candidate, hashed: true);
        }

        return _catalog.ConnectionString(physical!);
    }

    static IQueryable<DatabaseNameBinding> Query(ClusterDbContext context, DatabaseIdentity identity) =>
        context.DatabaseNames.AsNoTracking().Where(row => row.Kind == identity.Kind && row.EventStore == identity.EventStore && row.Namespace == identity.Namespace);

    static bool IsUniqueViolation(DbUpdateException exception) => exception.InnerException switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UniqueViolation,
        SqlException sql => sql.Number is 2601 or 2627,
        _ => false
    };

    async Task<string?> Insert(ClusterDbContext context, DatabaseIdentity identity, string name, bool hashed)
    {
        var binding = new DatabaseNameBinding { Kind = identity.Kind, EventStore = identity.EventStore, Namespace = identity.Namespace, PhysicalName = name };
        context.DatabaseNames.Add(binding);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            context.Entry(binding).State = EntityState.Detached;
            var winner = await Query(context, identity).SingleOrDefaultAsync();
            if (winner is not null)
            {
                name = winner.PhysicalName;
                SqlStorageTelemetry.RecoveredCollision("database-name");
            }
            else if (hashed)
            {
                throw new DatabaseNameBindingFailure($"DatabaseNames collision for hashed database '{name}' belonging to {identity}. Another identity already owns this SHA-256 prefix.");
            }
            else
            {
                return null;
            }
        }

        _bindings.TryAdd(identity, name);
        return name;
    }

    async Task<string> Separate(DatabaseIdentity identity, string candidate, IEnumerable<DatabaseIdentity> claimants, DatabaseOwnership ownership, ILogger<DatabaseNameResolver> logger)
    {
        if (await _catalog.Exists(candidate) && await ownership.HasData(identity.Kind, candidate))
        {
            throw new DatabaseOwnershipUnresolved(identity, candidate, claimants);
        }

        var name = DatabaseName.Hashed(identity.LogicalName(_catalog.ClusterName), _catalog.Provider);
        logger.SharedDatabase(identity.ToString(), candidate, name);
        return name;
    }
}
