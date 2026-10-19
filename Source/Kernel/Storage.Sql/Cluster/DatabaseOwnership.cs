// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Discovers competing logical identities and schema evidence without migrating their databases.
/// </summary>
/// <param name="catalog">The provider metadata reader.</param>
/// <param name="eventStore">Creates an unmigrated event store context.</param>
/// <param name="namespace">Creates an unmigrated namespace context.</param>
internal sealed class DatabaseOwnership(SqlDatabaseCatalog catalog, Func<string, EventStoreDbContext> eventStore, Func<string, NamespaceDbContext> @namespace)
{
    static readonly string[] _eventColumns = ["SequenceNumber", "EventSourceId", "EventSourceType", "Type", "Content", "Occurred", "Causation", "CausedBy", "CorrelationId"];
    static readonly string[] _constraintColumns = ["EventSourceId", "Value", "SequenceNumber"];

    /// <summary>
    /// Lists every configured identity that could name the candidate database.
    /// </summary>
    /// <param name="cluster">The cluster metadata.</param>
    /// <param name="requested">The identity being resolved, whether listed yet or not.</param>
    /// <param name="candidate">The unchanged or legacy physical name.</param>
    /// <returns>The competing identities.</returns>
    internal async Task<HashSet<DatabaseIdentity>> Claimants(ClusterDbContext cluster, DatabaseIdentity requested, string candidate)
    {
        var identities = new HashSet<DatabaseIdentity> { requested, DatabaseIdentity.Cluster };
        var stores = await cluster.EventStores.Select(store => store.Name).ToListAsync();
        foreach (var store in stores.Append(requested.EventStore).Distinct(StringComparer.Ordinal))
        {
            var storeIdentity = DatabaseIdentity.Store(store);
            identities.Add(storeIdentity);
            var namespaces = new HashSet<string>(StringComparer.Ordinal) { EventStoreNamespaceName.Default.Value };
            if (store == requested.EventStore)
            {
                namespaces.Add(requested.Namespace);
            }

            var binding = await cluster.DatabaseNames.AsNoTracking().SingleOrDefaultAsync(row => row.Kind == DatabaseKind.EventStore && row.EventStore == store && row.Namespace == EventStoreNamespaceName.Default.Value);
            var name = binding?.PhysicalName ?? storeIdentity.LogicalName(catalog.ClusterName);
            if (binding is null && !DatabaseName.Fits(name, catalog.Provider))
            {
                var legacy = DatabaseName.Clip(name, catalog.Provider, DatabaseName.Limit(catalog.Provider));
                name = catalog.Provider == DatabaseType.PostgreSql && await catalog.Exists(legacy) ? legacy : DatabaseName.Hashed(name, catalog.Provider);
            }

            if (await catalog.Exists(name) && (await catalog.Tables(name)).ContainsKey(WellKnownTableNames.Namespaces))
            {
                await using var context = eventStore(catalog.ConnectionString(name));
                namespaces.UnionWith(await context.Namespaces.Select(ns => ns.Name).ToListAsync());
            }

            foreach (var ns in namespaces)
            {
                identities.Add(new(DatabaseKind.Namespace, store, ns));
                identities.Add(new(DatabaseKind.ReadModels, store, ns));
            }
        }

        var result = new HashSet<DatabaseIdentity>();
        foreach (var identity in identities)
        {
            var logical = identity.LogicalName(catalog.ClusterName);
            var legacy = catalog.Provider == DatabaseType.PostgreSql ? DatabaseName.Clip(logical, catalog.Provider, 63) : logical;
            if (await catalog.Equal(legacy, candidate))
            {
                result.Add(identity);
            }
        }

        return result;
    }

    /// <summary>
    /// Tests whether the shared database has schema or history that could belong to the losing identity.
    /// </summary>
    /// <param name="kind">The losing schema kind.</param>
    /// <param name="name">The existing shared database.</param>
    /// <returns>Whether automatically selecting an empty database could strand data.</returns>
    internal async Task<bool> HasData(DatabaseKind kind, string name)
    {
        var tables = await catalog.Tables(name);
        var history = tables.ContainsKey("__EFMigrationsHistory") ? await catalog.MigrationHistory(name) : [];
        await using var storeContext = eventStore(catalog.ConnectionString(name));
        await using var namespaceContext = @namespace(catalog.ConnectionString(name));
        var storeTables = storeContext.Model.GetEntityTypes().Select(entity => entity.GetTableName()).ToHashSet(StringComparer.Ordinal);
        var namespaceTables = namespaceContext.Model.GetEntityTypes().Select(entity => entity.GetTableName()).ToHashSet(StringComparer.Ordinal);

        // Dynamic table names are supplied by the application, but their engine-owned column shapes are fixed.
        namespaceTables.UnionWith(tables.Where(pair => IsNamespaceTable(pair.Value)).Select(pair => pair.Key));
        namespaceTables.UnionWith(["Jobs", "JobSteps", "FailedJobSteps", "__cratis_named_tags"]);
        return kind switch
        {
            DatabaseKind.EventStore => history.Any(migration => !IsNamespaceMigration(migration)) || tables.Keys.Any(storeTables.Contains),
            DatabaseKind.Namespace => history.Any(IsNamespaceMigration) || tables.Keys.Any(table => namespaceTables.Contains(table) && !storeTables.Contains(table)),
            _ => tables.Keys.Any(table => table != "__EFMigrationsHistory" && !storeTables.Contains(table) && !namespaceTables.Contains(table))
        };
    }

    /// <summary>
    /// Tests whether a migration belongs to a namespace database: the namespace schema (NS-*) or the Cratis.Orleans jobs schema (Cratis-Orleans-Jobs-*).
    /// </summary>
    /// <param name="migration">The migration identifier.</param>
    /// <returns>Whether the migration belongs to a namespace database.</returns>
    static bool IsNamespaceMigration(string migration) =>
        migration.StartsWith("NS-", StringComparison.Ordinal) || migration.StartsWith("Cratis-Orleans-Jobs-", StringComparison.Ordinal);

    static bool IsNamespaceTable(string[] columns) =>
        _eventColumns.All(column => columns.Contains(column, StringComparer.Ordinal)) ||
        (columns.Length is 3 or 4 &&
            _constraintColumns.All(column => columns.Contains(column, StringComparer.Ordinal)) &&
            columns.All(column => _constraintColumns.Contains(column, StringComparer.Ordinal) || column == "Subject"));
}
