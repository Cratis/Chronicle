// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.Connections;

/// <summary>
/// Resolves the SQL connections of the kernel through the configured connection resolution.
/// </summary>
/// <remarks>
/// Holds the one <see cref="ICachingConnectionResolver"/> of the process, so every database shares its cache. When no
/// resolution is configured, every resolution is <see cref="ResolvedConnection.Unchanged"/> and nothing is ever called.
/// </remarks>
/// <param name="storage">The <see cref="Storage"/> configuration.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> the resolver is created through.</param>
internal sealed class SqlConnectionResolution(Configuration.Storage storage, IServiceProvider serviceProvider)
{
    readonly Lazy<ICachingConnectionResolver> _resolver = new(() =>
        (serviceProvider.GetService<IConnectionResolverFactory>() ?? new ConnectionResolverFactory(serviceProvider)).Create(storage.ConnectionResolution));

    readonly string _storage = storage.ConnectionDetails.GetDatabaseType() switch
    {
        DatabaseType.SqlServer => WellKnownConnectionStorage.SqlServer,
        DatabaseType.PostgreSql => WellKnownConnectionStorage.PostgreSql,
        _ => WellKnownConnectionStorage.Sqlite
    };

    /// <summary>
    /// Gets a value indicating whether any connection is resolved.
    /// </summary>
    internal bool IsConfigured => storage.ConnectionResolution.IsConfigured;

    /// <summary>
    /// Gets the resolver.
    /// </summary>
    internal ICachingConnectionResolver Resolver => _resolver.Value;

    /// <summary>
    /// Gets the instance registered with a service provider, or a new one when none is.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="storage">The <see cref="Storage"/> configuration.</param>
    /// <returns>The <see cref="SqlConnectionResolution"/>.</returns>
    internal static SqlConnectionResolution From(IServiceProvider serviceProvider, Configuration.Storage storage) =>
        serviceProvider.GetService<SqlConnectionResolution>() ?? new(storage, serviceProvider);

    /// <summary>
    /// Create the request for a database and purpose.
    /// </summary>
    /// <param name="identity">The <see cref="DatabaseIdentity"/>.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <returns>The <see cref="ConnectionRequest"/>.</returns>
    internal ConnectionRequest RequestFor(DatabaseIdentity identity, ConnectionPurpose purpose) => identity.Kind switch
    {
        DatabaseKind.Cluster => StorageConnectionRequests.For(_storage, purpose, StorageConnectionKind.Cluster),
        DatabaseKind.EventStore => StorageConnectionRequests.For(_storage, purpose, StorageConnectionKind.EventStore, identity.EventStore),
        DatabaseKind.Namespace => StorageConnectionRequests.For(_storage, purpose, StorageConnectionKind.Namespace, identity.EventStore, identity.Namespace),
        _ => StorageConnectionRequests.For(_storage, purpose, StorageConnectionKind.ReadModels, identity.EventStore, identity.Namespace)
    };

    /// <summary>
    /// Resolve the connection for a database and purpose.
    /// </summary>
    /// <param name="identity">The <see cref="DatabaseIdentity"/>.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <returns>The <see cref="ResolvedConnection"/>.</returns>
    internal Task<ResolvedConnection> Resolve(DatabaseIdentity identity, ConnectionPurpose purpose) =>
        IsConfigured ? Resolver.Resolve(RequestFor(identity, purpose)) : Task.FromResult(ResolvedConnection.Unchanged);

    /// <summary>
    /// Get the schema for a resolved connection.
    /// </summary>
    /// <param name="resolved">The <see cref="ResolvedConnection"/>.</param>
    /// <returns>The resolved schema, else the configured one. Empty means the default schema of the database user.</returns>
    internal string SchemaFor(ResolvedConnection resolved) => resolved.Schema ?? storage.Schema;

    /// <summary>
    /// Apply the resolved identity to a connection Chronicle opens itself to inspect or create databases.
    /// </summary>
    /// <param name="connection">The closed <see cref="DbConnection"/>.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// These connections administer the server — checking which databases exist and creating them — so they use the
    /// identity resolved for the cluster's <see cref="ConnectionPurpose.Schema"/>.
    /// </remarks>
    internal async Task PrepareRawConnection(DbConnection connection)
    {
        if (!IsConfigured)
        {
            return;
        }

        var resolved = await Resolve(DatabaseIdentity.Cluster, ConnectionPurpose.Schema);
        connection.ApplyResolvedConnection(connection.ConnectionString, new ResolvedConnection(Identity: resolved.Identity));
    }
}
