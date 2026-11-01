// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The part of <see cref="Database"/> that reads the metadata of every scope that could own a table name, without writing anything.
/// </summary>
public partial class Database
{
    static async Task ReadNames(DbContext context, Func<Task> read)
    {
        try
        {
            await read();
        }
        catch (Exception error) when (SqlErrors.IsMissingTable(error))
        {
            if (!await SqlErrors.MissingTableIsVisible(context))
            {
                throw;
            }

            // A visible database that predates this table has nothing registered through it.
        }
    }

    async Task<IReadOnlyList<ReadModelTableMetadataScope>> ReadModelTableMetadataScopes(DatabaseIdentity requested, ReadModelTablePlacement? placement)
    {
        var scopes = new List<ReadModelTableMetadataScope>();
        try
        {
            await using var cluster = new ClusterDbContext(ReadOptions<ClusterDbContext>(ClusterTarget(ConnectionPurpose.Runtime)));
            var clusterExists = await cluster.GetService<IRelationalDatabaseCreator>().ExistsAsync();
            foreach (var store in (await EventStoreNames(cluster, clusterExists)).Append(requested.EventStore).Distinct(StringComparer.Ordinal))
            {
                await AddStoreScopes(cluster, store, requested, placement, scopes);
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            scopes.Add(ReadModelTableMetadataScope.Unknown("*", null, "cluster", error.GetType().Name));
        }

        return scopes;
    }

    async Task<IReadOnlyList<string>> EventStoreNames(ClusterDbContext cluster, bool clusterExists)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (clusterExists)
        {
            await ReadNames(cluster, async () => names.UnionWith(await cluster.EventStores.AsNoTracking().Select(store => store.Name).ToArrayAsync()));
        }

        var clusterTarget = ClusterTarget(ConnectionPurpose.Runtime);
        if (clusterTarget.ConnectionString.GetDatabaseType() == DatabaseType.Sqlite)
        {
            // The directory listing is positive visibility, unlike File.Exists. Permission failures must escape to Unknown.
            var path = new SqliteConnectionStringBuilder(clusterTarget.ConnectionString).DataSource;
            var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
            var prefix = Path.GetFileNameWithoutExtension(path) + "+es+";
            var extension = Path.GetExtension(path);
            foreach (var file in Directory.EnumerateFileSystemEntries(directory))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    var name = fileName[prefix.Length..^extension.Length];
                    if (!name.Contains('+'))
                    {
                        names.Add(name);
                    }
                }
            }
        }

        return [.. names];
    }

    async Task AddStoreScopes(ClusterDbContext cluster, string store, DatabaseIdentity requested, ReadModelTablePlacement? placement, List<ReadModelTableMetadataScope> scopes)
    {
        var located = await LocateReadOnly(cluster, DatabaseIdentity.Store(store));
        if (located.Unknown is not null)
        {
            scopes.Add(ReadModelTableMetadataScope.Unknown(store, null, located.Unknown.Stage, located.Unknown.Reason));
            return;
        }

        var names = new List<string> { EventStoreNamespaceName.Default.Value };
        if (store == requested.EventStore)
        {
            names.Add(requested.Namespace);
        }

        if (located.Target is not null)
        {
            try
            {
                await using var definitions = new EventStoreDbContext(ReadOptions<EventStoreDbContext>(located.Target));
                await ReadNames(definitions, async () => names.AddRange(await definitions.Namespaces.AsNoTracking().Select(ns => ns.Name).ToArrayAsync()));
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                scopes.Add(ReadModelTableMetadataScope.Unknown(store, null, "namespaces", error.GetType().Name));
                return;
            }
        }

        foreach (var name in names.Distinct(StringComparer.Ordinal))
        {
            await AddCandidateScope(cluster, new(DatabaseKind.ReadModels, store, name), requested, placement, located.Target, scopes);
        }
    }

    async Task AddCandidateScope(ClusterDbContext cluster, DatabaseIdentity candidate, DatabaseIdentity requested, ReadModelTablePlacement? placement, SqlTarget? storeTarget, List<ReadModelTableMetadataScope> scopes)
    {
        var inclusion = await ReadModelTableScopeInclusion(cluster, candidate, requested, placement);
        if (inclusion is DatabaseLocation.Absent)
        {
            return;
        }

        if (inclusion is DatabaseLocation.Unknown unknown)
        {
            scopes.Add(ReadModelTableMetadataScope.Unknown(candidate.EventStore, candidate.Namespace, unknown.Stage, unknown.Reason));
            return;
        }

        var history = await LocateReadOnly(cluster, candidate with { Kind = DatabaseKind.Namespace });
        if (history.Unknown is not null)
        {
            scopes.Add(ReadModelTableMetadataScope.Unknown(candidate.EventStore, candidate.Namespace, history.Unknown.Stage, history.Unknown.Reason));
            return;
        }

#pragma warning disable CA2000 // Successful construction transfers ownership to the returned scope; ReadModelTableOwnership.Read disposes every scope.
        var definitions = storeTarget is null ? null : new EventStoreDbContext(ReadOptions<EventStoreDbContext>(storeTarget));
        NamespaceDbContext? historyContext = null;
        try
        {
            historyContext = history.Target is null ? null : new NamespaceDbContext(ReadOptions<NamespaceDbContext>(history.Target));
            scopes.Add(new(candidate.EventStore, candidate.Namespace, definitions, historyContext));
        }
        catch
        {
            if (definitions is not null)
            {
                await definitions.DisposeAsync();
            }

            if (historyContext is not null)
            {
                await historyContext.DisposeAsync();
            }

            throw;
        }
#pragma warning restore CA2000
    }
}
