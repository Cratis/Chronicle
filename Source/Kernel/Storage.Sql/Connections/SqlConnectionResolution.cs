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
/// Adapts the storage connection resolution to the SQL databases of the kernel.
/// </summary>
/// <remarks>
/// Resolution itself — the resolver, its cache and the rule that the cluster keeps its configured placement — is the
/// storage-agnostic <see cref="IStorageConnectionResolution"/>. This adds what only SQL needs: mapping a
/// <see cref="DatabaseIdentity"/> to a request, the schema, and the identity for connections Chronicle opens itself.
/// </remarks>
/// <param name="storage">The <see cref="Storage"/> configuration.</param>
/// <param name="resolution">The <see cref="IStorageConnectionResolution"/>.</param>
internal sealed class SqlConnectionResolution(Configuration.Storage storage, IStorageConnectionResolution resolution)
{
    /// <summary>
    /// Gets a value indicating whether any connection is resolved.
    /// </summary>
    internal bool IsConfigured => resolution.IsConfigured;

    /// <summary>
    /// Gets the resolver.
    /// </summary>
    internal ICachingConnectionResolver Resolver => resolution.Resolver;

    /// <summary>
    /// Get the kind of storage a SQL connection string connects to.
    /// </summary>
    /// <param name="connectionDetails">The configured connection string.</param>
    /// <returns>One of <see cref="WellKnownConnectionStorage"/>.</returns>
    internal static string StorageFor(string connectionDetails) => connectionDetails.GetDatabaseType() switch
    {
        DatabaseType.SqlServer => WellKnownConnectionStorage.SqlServer,
        DatabaseType.PostgreSql => WellKnownConnectionStorage.PostgreSql,
        _ => WellKnownConnectionStorage.Sqlite
    };

    /// <summary>
    /// Gets the instance registered with a service provider, or a new one when none is.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="storage">The <see cref="Storage"/> configuration.</param>
    /// <returns>The <see cref="SqlConnectionResolution"/>.</returns>
    internal static SqlConnectionResolution From(IServiceProvider serviceProvider, Configuration.Storage storage)
    {
        if (serviceProvider.GetService<SqlConnectionResolution>() is { } registered)
        {
            return registered;
        }

        var resolution = serviceProvider.GetService<IStorageConnectionResolution>() ?? new StorageConnectionResolution(
            StorageFor(storage.ConnectionDetails),
            storage.ConnectionResolution,
            serviceProvider.GetService<IConnectionResolverFactory>() ?? new ConnectionResolverFactory(serviceProvider));
        return new(storage, resolution);
    }

    /// <summary>
    /// Create the request for a database and purpose.
    /// </summary>
    /// <param name="identity">The <see cref="DatabaseIdentity"/>.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <returns>The <see cref="ConnectionRequest"/>.</returns>
    internal ConnectionRequest RequestFor(DatabaseIdentity identity, ConnectionPurpose purpose)
    {
        var (kind, eventStore, @namespace) = Scope(identity);
        return resolution.RequestFor(purpose, kind, eventStore, @namespace);
    }

    /// <summary>
    /// Resolve the connection for a database and purpose.
    /// </summary>
    /// <param name="identity">The <see cref="DatabaseIdentity"/>.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <returns>The <see cref="ResolvedConnection"/>.</returns>
    internal Task<ResolvedConnection> Resolve(DatabaseIdentity identity, ConnectionPurpose purpose)
    {
        var (kind, eventStore, @namespace) = Scope(identity);
        return resolution.Resolve(purpose, kind, eventStore, @namespace);
    }

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
    internal Task PrepareRawConnection(DbConnection connection) => Prepare(connection, ConnectionPurpose.Schema);

    /// <summary>
    /// Apply the resolved identity to a connection Chronicle opens itself to check the cluster database is reachable.
    /// </summary>
    /// <param name="connection">The closed <see cref="DbConnection"/>.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Uses the identity resolved for the cluster's <see cref="ConnectionPurpose.Runtime"/>, the one the kernel runs with.
    /// </remarks>
    internal Task PrepareRuntimeConnection(DbConnection connection) => Prepare(connection, ConnectionPurpose.Runtime);

    static (StorageConnectionKind Kind, string EventStore, string Namespace) Scope(DatabaseIdentity identity) => identity.Kind switch
    {
        DatabaseKind.Cluster => (StorageConnectionKind.Cluster, string.Empty, string.Empty),
        DatabaseKind.EventStore => (StorageConnectionKind.EventStore, identity.EventStore, string.Empty),
        DatabaseKind.Namespace => (StorageConnectionKind.Namespace, identity.EventStore, identity.Namespace),
        _ => (StorageConnectionKind.ReadModels, identity.EventStore, identity.Namespace)
    };

    async Task Prepare(DbConnection connection, ConnectionPurpose purpose)
    {
        if (!IsConfigured)
        {
            return;
        }

        var resolved = await Resolve(DatabaseIdentity.Cluster, purpose);
        connection.ApplyResolvedConnection(connection.ConnectionString, new ResolvedConnection(Identity: resolved.Identity));
    }
}
