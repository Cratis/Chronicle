// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents an implementation of <see cref="IStorageConnectionResolution"/>.
/// </summary>
/// <param name="storage">The kind of storage, one of <see cref="WellKnownConnectionStorage"/>.</param>
/// <param name="options">The configured <see cref="ConnectionResolutionOptions"/>.</param>
/// <param name="factory">The <see cref="IConnectionResolverFactory"/> creating the resolver.</param>
public class StorageConnectionResolution(string storage, ConnectionResolutionOptions options, IConnectionResolverFactory factory) : IStorageConnectionResolution
{
    readonly Lazy<ICachingConnectionResolver> _resolver = new(() => factory.Create(options));

    /// <inheritdoc/>
    public bool IsConfigured => options.IsConfigured;

    /// <inheritdoc/>
    public ICachingConnectionResolver Resolver => _resolver.Value;

    /// <inheritdoc/>
    public ConnectionRequest RequestFor(ConnectionPurpose purpose, StorageConnectionKind kind, string eventStore = "", string @namespace = "") =>
        StorageConnectionRequests.For(storage, purpose, kind, eventStore, @namespace);

    /// <inheritdoc/>
    public async Task<ResolvedConnection> Resolve(ConnectionPurpose purpose, StorageConnectionKind kind, string eventStore = "", string @namespace = "")
    {
        if (!IsConfigured)
        {
            return ResolvedConnection.Unchanged;
        }

        var resolved = await Resolver.Resolve(RequestFor(purpose, kind, eventStore, @namespace));

        // The cluster database is always the configured one; only who connects to it, and its schema, are resolved.
        return kind == StorageConnectionKind.Cluster
            ? resolved with { ConnectionString = null, Database = null }
            : resolved;
    }
}
