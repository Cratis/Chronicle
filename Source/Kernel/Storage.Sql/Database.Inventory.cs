// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The read-only entry points used to find out which databases are in use.
/// </summary>
public partial class Database
{
    /// <summary>
    /// Reads every permanent database binding, without binding anything.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that stops the read.</param>
    /// <returns>The physical database name of each bound identity. Empty for providers that do not bind names.</returns>
    internal async Task<IReadOnlyDictionary<DatabaseIdentity, string>> Bindings(CancellationToken cancellationToken)
    {
        if (!UsesBindings)
        {
            return new Dictionary<DatabaseIdentity, string>();
        }

        await using var context = ReadOnlyCluster();
        var rows = await context.DatabaseNames.AsNoTracking().ToListAsync(cancellationToken);
        return rows.ToDictionary(_ => _.Identity, _ => _.PhysicalName);
    }

    /// <summary>
    /// Reads the names of the registered event stores.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that stops the read.</param>
    /// <returns>The names.</returns>
    internal async Task<IReadOnlyList<string>> RegisteredEventStores(CancellationToken cancellationToken)
    {
        await using var context = ReadOnlyCluster();
        return await context.EventStores.AsNoTracking().Select(_ => _.Name).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets where a database is, and how to connect to it, without creating, binding or migrating anything.
    /// </summary>
    /// <param name="identity">The <see cref="DatabaseIdentity"/>.</param>
    /// <param name="bindings">The bindings read by <see cref="Bindings"/>.</param>
    /// <returns>The <see cref="SqlTarget"/>.</returns>
    /// <remarks>
    /// The order is the one the runtime uses: a resolved placement, then the binding, then the name a binding would be given.
    /// </remarks>
    internal async Task<SqlTarget> EffectiveTarget(DatabaseIdentity identity, IReadOnlyDictionary<DatabaseIdentity, string> bindings)
    {
        if (identity.Kind == DatabaseKind.Cluster)
        {
            return ClusterTarget(ConnectionPurpose.Runtime);
        }

        var resolved = await _resolution.Resolve(identity, ConnectionPurpose.Runtime);
        string connectionString;
        if (SqlConnectionStrings.OwnsPlacement(resolved))
        {
            connectionString = SqlConnectionStrings.WithPlacement(options.Value.Storage.ConnectionDetails, resolved);
        }
        else if (UsesBindings)
        {
            var name = bindings.TryGetValue(identity, out var bound) ? bound : await _databaseNames.Candidate(identity);
            connectionString = _databaseNames.ConnectionStringFor(name);
        }
        else
        {
            connectionString = ConnectionStringFor(identity.LogicalName(string.Empty));
        }

        return TargetFor(identity, ConnectionPurpose.Runtime, connectionString, resolved);
    }

    /// <summary>
    /// Checks whether a resolution decides where a database is.
    /// </summary>
    /// <param name="identity">The <see cref="DatabaseIdentity"/>.</param>
    /// <returns>True when a connection string or database is resolved for it.</returns>
    internal async Task<bool> IsPlaced(DatabaseIdentity identity) => SqlConnectionStrings.OwnsPlacement(await _resolution.Resolve(identity, ConnectionPurpose.Runtime));

    /// <summary>
    /// Groups the database names the server treats as equal.
    /// </summary>
    /// <param name="names">The database names.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that stops the query.</param>
    /// <returns>Each name with its representative.</returns>
    internal Task<IReadOnlyDictionary<string, string>> Representatives(IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) => _databaseNames.Groups(names, cancellationToken);

    /// <summary>
    /// Reads the names of the namespaces recorded in an event store database, if it exists.
    /// </summary>
    /// <param name="target">Where it is.</param>
    /// <param name="placed">Whether a resolution decided where it is, which is then not checked for existence here.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that stops the read.</param>
    /// <returns>The namespace names. Empty when the database or its table does not exist.</returns>
    internal async Task<IReadOnlyList<string>> NamespacesOf(SqlTarget target, bool placed, CancellationToken cancellationToken)
    {
        if (!await Exists(target, placed, cancellationToken))
        {
            return [];
        }

        await using var context = new EventStoreDbContext(ReadOptions<EventStoreDbContext>(target));
        try
        {
            return await context.Namespaces.AsNoTracking().Select(_ => _.Name).ToListAsync(cancellationToken);
        }
        catch (Exception exception) when (IsMissingTable(exception))
        {
            return [];
        }
    }

    static bool IsMissingTable(Exception exception) => exception switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UndefinedTable,
        SqlException sql => sql.Number == 208,
        SqliteException sqlite => sqlite.SqliteErrorCode == 1 && sqlite.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase),
        _ => exception.InnerException is not null && IsMissingTable(exception.InnerException)
    };

    /// <summary>
    /// Creates a context on the cluster database that only reads: nothing is migrated and no name is reserved.
    /// </summary>
    /// <returns>The <see cref="ClusterDbContext"/>, owned by the caller.</returns>
    ClusterDbContext ReadOnlyCluster() => new(ReadOptions<ClusterDbContext>(ClusterTarget(ConnectionPurpose.Runtime)));

    async Task<bool> Exists(SqlTarget target, bool placed, CancellationToken cancellationToken)
    {
        switch (target.ConnectionString.GetDatabaseType())
        {
            case DatabaseType.Sqlite:
                return File.Exists(new SqliteConnectionStringBuilder(target.ConnectionString).DataSource);
            case DatabaseType.PostgreSql when !placed:
                return await _databaseNames.Exists(new NpgsqlConnectionStringBuilder(target.ConnectionString).Database!, cancellationToken);
            case DatabaseType.SqlServer when !placed:
                return await _databaseNames.Exists(new SqlConnectionStringBuilder(target.ConnectionString).InitialCatalog, cancellationToken);
            default:
                return true;
        }
    }
}
