// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Reads generation metadata without provisioning, migration, or SQLite write pragmas.
/// </summary>
public partial class Database
{
    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventStoreName, GenerationFloor>> ReadGenerationFloors()
    {
        var result = new Dictionary<EventStoreName, GenerationFloor>();
        foreach (var pair in await GenerationStoreTargets())
        {
            var target = pair.Value;

            // Disposed by the await using declaration; the analyzer does not track it through the async probe.
#pragma warning disable CA2000
            await using var probe = new EventStoreDbContext(ReadOptions<EventStoreDbContext>(target));
#pragma warning restore CA2000
            if (!await probe.GetService<IRelationalDatabaseCreator>().ExistsAsync())
            {
                continue;
            }

            // DbContext ownership transfers to DbContextScope, which disposes it.
#pragma warning disable CA2000
            var floorStorage = new EventStores.Generations.GenerationFloorStorage(() => Task.FromResult(new DbContextScope<EventStoreDbContext>(new(ReadOptions<EventStoreDbContext>(target)), static () => { })));
#pragma warning restore CA2000
            try
            {
                if (await floorStorage.Get() is { } floor)
                {
                    result.Add(pair.Key, floor);
                }
            }
            catch (Exception error) when (MissingGenerationTable(error))
            {
                // A legacy database without the floor table imposes no generation requirement.
            }
        }
        return result;
    }

    static bool MissingGenerationTable(Exception error) => error switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UndefinedTable,
        SqlException sql => sql.Number == 208,
        SqliteException sqlite => sqlite.SqliteErrorCode == 1 && sqlite.Message.Contains("no such table", StringComparison.Ordinal),
        _ => false
    };

    async Task<Dictionary<EventStoreName, SqlTarget>> GenerationStoreTargets()
    {
        var result = new Dictionary<EventStoreName, SqlTarget>();
        var clusterTarget = await Target(DatabaseIdentity.Cluster, ConnectionPurpose.Runtime);
        await using var cluster = new ClusterDbContext(ReadOptions<ClusterDbContext>(clusterTarget));
        if (await cluster.GetService<IRelationalDatabaseCreator>().ExistsAsync())
        {
            try
            {
                foreach (var binding in await cluster.DatabaseNames.AsNoTracking().Where(row => row.Kind == DatabaseKind.EventStore).ToArrayAsync())
                {
                    result[new(binding.EventStore)] = await GenerationTarget(binding.Identity, binding.PhysicalName);
                }
            }
            catch (Exception error) when (MissingGenerationTable(error))
            {
                // Pre-binding installations have no advanced floors outside their existing registry.
            }
            if (!UsesBindings)
            {
                try
                {
                    foreach (var store in await cluster.EventStores.AsNoTracking().ToArrayAsync())
                    {
                        result[new(store.Name)] = await Target(DatabaseIdentity.Store(store.Name), ConnectionPurpose.Runtime);
                    }
                }
                catch (Exception error) when (MissingGenerationTable(error))
                {
                    // A fresh cluster has no registry yet.
                }
            }
        }
        if (clusterTarget.ConnectionString.GetDatabaseType() == DatabaseType.Sqlite)
        {
            var path = new SqliteConnectionStringBuilder(clusterTarget.ConnectionString).DataSource;
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            var prefix = Path.GetFileNameWithoutExtension(path) + "+es+";
            var extension = Path.GetExtension(path);
            foreach (var file in Directory.Exists(directory) ? Directory.EnumerateFiles(directory, prefix + "*" + extension) : [])
            {
                var name = Path.GetFileName(file)[prefix.Length..^extension.Length];
                if (!name.Contains('+'))
                {
                    result[new(name)] = new(new SqliteConnectionStringBuilder(clusterTarget.ConnectionString) { DataSource = file, Mode = SqliteOpenMode.ReadOnly }.ConnectionString, clusterTarget.Schema, null);
                }
            }
        }
        return result;
    }

    async Task<SqlTarget> GenerationTarget(DatabaseIdentity identity, string physicalName)
    {
        var resolved = await _resolution.Resolve(identity, ConnectionPurpose.Runtime);
        var connectionString = SqlConnectionStrings.OwnsPlacement(resolved)
            ? SqlConnectionStrings.WithPlacement(options.Value.Storage.ConnectionDetails, resolved)
            : new SqlDatabaseCatalog(options.Value.Storage.ConnectionDetails).ConnectionString(physicalName);
        return TargetFor(identity, ConnectionPurpose.Runtime, connectionString, resolved);
    }

    DbContextOptions<T> ReadOptions<T>(SqlTarget target)
        where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>();
        var connectionString = target.ConnectionString.GetDatabaseType() == DatabaseType.Sqlite
            ? new SqliteConnectionStringBuilder(target.ConnectionString) { Mode = SqliteOpenMode.ReadOnly }.ConnectionString
            : target.ConnectionString;
        builder.UseDatabaseFromConnectionString(connectionString).UseApplicationServiceProvider(serviceProvider).AddConceptAsSupport();
        if (target.Request is not null)
        {
            builder.AddInterceptors(new ResolvedIdentityInterceptor(_resolution.Resolver, target.Request));
        }
        return builder.Options;
    }
}
