// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Sql.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents a read-only <see cref="IResourceInventory"/> for the SQL backends.
/// </summary>
/// <remarks>
/// The owners are the event stores in the registry, the owners named by the permanent database bindings and always the System event store.
/// Each database is identified by the server, the database as that provider compares database names, and the schema:
/// PostgreSQL by ordinal name, the schema without regard to case (as provisioning compares it), SQL Server by the representative the server's collation gives the name, and SQLite by the normalized
/// full path of the file. Nothing is bound, created, migrated or reserved: the cluster is read through a context that only reads.
/// </remarks>
/// <param name="database">The <see cref="Database"/>.</param>
internal sealed class SqlResourceInventory(Database database) : IResourceInventory
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ResourceUse>> Read(CancellationToken cancellationToken)
    {
        var bindings = await database.Bindings(cancellationToken);
        var owners = await Owners(bindings, cancellationToken);
        var pending = new List<(ResourceOwner? Owner, ResourceKind Kind, SqlTarget Target, DatabaseIdentity Identity)>
        {
            (null, ResourceKind.Kernel, await database.EffectiveTarget(DatabaseIdentity.Cluster, bindings), DatabaseIdentity.Cluster)
        };

        foreach (var (eventStore, namespaces) in owners)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var owner = new ResourceOwner(eventStore);
            var storeIdentity = DatabaseIdentity.Store(eventStore);
            var storeTarget = await database.EffectiveTarget(storeIdentity, bindings);
            pending.Add((owner, ResourceKind.EventStoreDatabase, storeTarget, storeIdentity));

            var recorded = await database.NamespacesOf(storeTarget, await IsPlaced(storeIdentity), cancellationToken);
            foreach (var @namespace in namespaces.Concat(recorded).Distinct(StringComparer.Ordinal))
            {
                var namespaceOwner = new ResourceOwner(eventStore, @namespace);
                var namespaceIdentity = new DatabaseIdentity(DatabaseKind.Namespace, eventStore, @namespace);
                var readModelsIdentity = new DatabaseIdentity(DatabaseKind.ReadModels, eventStore, @namespace);
                pending.Add((namespaceOwner, ResourceKind.NamespaceDatabase, await database.EffectiveTarget(namespaceIdentity, bindings), namespaceIdentity));
                pending.Add((namespaceOwner, ResourceKind.ReadModelDatabase, await database.EffectiveTarget(readModelsIdentity, bindings), readModelsIdentity));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var representatives = await database.Representatives([.. pending.Select(_ => DatabaseOf(_.Target.ConnectionString)).Distinct(StringComparer.Ordinal)], cancellationToken);
        return [.. pending.Select(_ => new ResourceUse(_.Owner, _.Kind, IdentityOf(_.Target, representatives)))];
    }

    /// <summary>
    /// Compose the identity of a database from where it is.
    /// </summary>
    /// <param name="target">The <see cref="SqlTarget"/>.</param>
    /// <param name="representatives">The representative the server gives each database name; only SQL Server uses it.</param>
    /// <returns>The identity.</returns>
    internal static string IdentityOf(SqlTarget target, IReadOnlyDictionary<string, string> representatives)
    {
        var connectionString = target.ConnectionString;
        switch (connectionString.GetDatabaseType())
        {
            case DatabaseType.PostgreSql:
                var postgres = new NpgsqlConnectionStringBuilder(connectionString);
                return $"pg:{postgres.Host!.ToLowerInvariant()}:{postgres.Port}|{postgres.Database}|{SchemaOf(target)}";
            case DatabaseType.SqlServer:
                var sql = new SqlConnectionStringBuilder(connectionString);
                var name = representatives.TryGetValue(sql.InitialCatalog, out var representative) ? representative : sql.InitialCatalog;
                return $"mssql:{sql.DataSource.ToLowerInvariant()}|{name}|{SchemaOf(target)}";
            default:
                var path = Path.GetFullPath(new SqliteConnectionStringBuilder(connectionString).DataSource).Normalize(NormalizationForm.FormC).ToUpperInvariant();
                return $"sqlite:{path}|{SchemaOf(target)}";
        }
    }

    static string SchemaOf(SqlTarget target) => target.Schema.ToUpperInvariant();

    static string DatabaseOf(string connectionString) => connectionString.GetDatabaseType() == DatabaseType.SqlServer
        ? new SqlConnectionStringBuilder(connectionString).InitialCatalog
        : string.Empty;

    async Task<IReadOnlyDictionary<string, HashSet<string>>> Owners(IReadOnlyDictionary<DatabaseIdentity, string> bindings, CancellationToken cancellationToken)
    {
        var owners = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            [EventStoreName.System.Value] = []
        };

        foreach (var name in await database.RegisteredEventStores(cancellationToken))
        {
            owners.TryAdd(name, []);
        }

        foreach (var identity in bindings.Keys.Where(_ => _.Kind != DatabaseKind.Cluster))
        {
            if (!owners.TryGetValue(identity.EventStore, out var namespaces))
            {
                owners[identity.EventStore] = namespaces = [];
            }

            if (identity.Kind != DatabaseKind.EventStore)
            {
                namespaces.Add(identity.Namespace);
            }
        }

        return owners;
    }

    Task<bool> IsPlaced(DatabaseIdentity identity) => database.IsPlaced(identity);
}
