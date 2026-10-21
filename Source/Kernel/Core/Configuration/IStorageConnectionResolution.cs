// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines the connection resolution of the configured storage, shared by every storage type.
/// </summary>
/// <remarks>
/// One instance per process holds the one resolver, so every database shares its cache, whether the storage is MongoDB
/// or SQL. The cluster database always stays the configured one: only its identity and schema are resolved.
/// </remarks>
public interface IStorageConnectionResolution
{
    /// <summary>
    /// Gets a value indicating whether any connection is resolved.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets the resolver, for refreshing or invalidating a resolution after it was applied.
    /// </summary>
    ICachingConnectionResolver Resolver { get; }

    /// <summary>
    /// Create the request for a database and purpose.
    /// </summary>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <param name="kind">The <see cref="StorageConnectionKind"/>.</param>
    /// <param name="eventStore">The event store, empty for the cluster.</param>
    /// <param name="namespace">The namespace, empty for the cluster and an event store.</param>
    /// <returns>The <see cref="ConnectionRequest"/>.</returns>
    ConnectionRequest RequestFor(ConnectionPurpose purpose, StorageConnectionKind kind, string eventStore = "", string @namespace = "");

    /// <summary>
    /// Resolve the connection for a database and purpose.
    /// </summary>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <param name="kind">The <see cref="StorageConnectionKind"/>.</param>
    /// <param name="eventStore">The event store, empty for the cluster.</param>
    /// <param name="namespace">The namespace, empty for the cluster and an event store.</param>
    /// <returns>The <see cref="ResolvedConnection"/>; <see cref="ResolvedConnection.Unchanged"/> when nothing is configured.</returns>
    Task<ResolvedConnection> Resolve(ConnectionPurpose purpose, StorageConnectionKind kind, string eventStore = "", string @namespace = "");
}
