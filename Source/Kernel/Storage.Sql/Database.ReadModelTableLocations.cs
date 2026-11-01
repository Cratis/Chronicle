// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The read-only location and inclusion decisions used while discovering possible read model table owners.
/// </summary>
public partial class Database
{
    static DatabaseLocation LocateSqlite(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var source = builder.DataSource;
        if (source == ":memory:" || builder.Mode == SqliteOpenMode.Memory || source.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return new DatabaseLocation.Unknown("probe", "unsupported data source");
        }

        var path = Path.GetFullPath(source);
        var directory = Path.GetDirectoryName(path)!;
        var name = Path.GetFileName(path);
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            if (string.Equals(Path.GetFileName(entry), name, StringComparison.OrdinalIgnoreCase))
            {
                builder.DataSource = entry;
                builder.Mode = SqliteOpenMode.ReadOnly;
                return new DatabaseLocation.Found(builder.ConnectionString);
            }
        }

        return new DatabaseLocation.Absent();
    }

    async Task<(SqlTarget? Target, DatabaseLocation.Unknown? Unknown)> LocateReadOnly(ClusterDbContext cluster, DatabaseIdentity identity)
    {
        var stage = "resolution";
        try
        {
            var resolved = await _resolution.Resolve(identity, ConnectionPurpose.Runtime);
            if (SqlConnectionStrings.OwnsPlacement(resolved))
            {
                return (TargetFor(identity, ConnectionPurpose.Runtime, SqlConnectionStrings.WithPlacement(options.Value.Storage.ConnectionDetails, resolved), resolved), null);
            }

            stage = "probe";
            var located = UsesBindings
                ? await _databaseNames.Locate(cluster, identity)
                : LocateSqlite(ConnectionStringFor(identity.LogicalName(string.Empty)));
            return located switch
            {
                DatabaseLocation.Found found => (MetadataTarget(found.ConnectionString), null),
                DatabaseLocation.Unknown unknown => (null, unknown),
                _ => (null, null)
            };
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return (null, new DatabaseLocation.Unknown(stage, error.GetType().Name));
        }
    }

    async Task<DatabaseLocation> ReadModelTableScopeInclusion(ClusterDbContext cluster, DatabaseIdentity candidate, DatabaseIdentity requested, ReadModelTablePlacement? placement)
    {
        try
        {
            if (candidate == requested)
            {
                return new DatabaseLocation.Found(string.Empty);
            }

            if (!_resolution.IsConfigured)
            {
                return await LegacyTableScopeInclusion(cluster, candidate, requested, placement);
            }

            var located = await LocateReadOnly(cluster, candidate);
            if (located.Unknown is not null)
            {
                return located.Unknown;
            }

            if (located.Target is null)
            {
                return new DatabaseLocation.Absent();
            }

            var other = ReadModelTablePlacement.Of(located.Target.ConnectionString, located.Target.Schema);
            return placement is not null && other is not null && !placement.Value.Overlaps(other.Value)
                ? new DatabaseLocation.Absent()
                : new DatabaseLocation.Found(located.Target.ConnectionString);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return new DatabaseLocation.Unknown("placement", error.GetType().Name);
        }
    }

    async Task<DatabaseLocation> LegacyTableScopeInclusion(ClusterDbContext cluster, DatabaseIdentity candidate, DatabaseIdentity requested, ReadModelTablePlacement? placement)
    {
        if (!UsesBindings)
        {
            return string.Equals(candidate.LogicalName(string.Empty), requested.LogicalName(string.Empty), StringComparison.OrdinalIgnoreCase)
                ? new DatabaseLocation.Found(string.Empty)
                : new DatabaseLocation.Absent();
        }

        var bound = false;
        await ReadNames(cluster, async () => bound = await cluster.DatabaseNames.AsNoTracking()
            .AnyAsync(row => row.Kind == candidate.Kind && row.EventStore == candidate.EventStore && row.Namespace == candidate.Namespace));
        if (bound)
        {
            // The cluster ledger's unique physical-name index prevents another bound scope sharing this placement.
            return new DatabaseLocation.Absent();
        }

        if (placement is null)
        {
            return new DatabaseLocation.Unknown("placement", "unparseable placement");
        }

        var catalog = new SqlDatabaseCatalog(options.Value.Storage.ConnectionDetails, _resolution.PrepareRawConnection);
        var logical = candidate.LogicalName(catalog.ClusterName);
        string[] names;
        if (DatabaseName.Fits(logical, catalog.Provider))
        {
            names = [logical];
        }
        else if (catalog.Provider == DatabaseType.PostgreSql)
        {
            names = [DatabaseName.Clip(logical, catalog.Provider, DatabaseName.Limit(catalog.Provider)), DatabaseName.Hashed(logical, catalog.Provider)];
        }
        else
        {
            names = [DatabaseName.Hashed(logical, catalog.Provider)];
        }

        foreach (var name in names)
        {
            if (await catalog.Equal(name, placement.Value.Database))
            {
                return new DatabaseLocation.Found(string.Empty);
            }
        }

        return new DatabaseLocation.Absent();
    }
}
