// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Identifies one immutable logical database binding.
/// </summary>
/// <param name="Kind">The owning schema.</param>
/// <param name="EventStore">The store name.</param>
/// <param name="Namespace">The explicit namespace, including Default.</param>
internal sealed record DatabaseIdentity(DatabaseKind Kind, string EventStore, string Namespace)
{
    /// <summary>
    /// Gets the reserved cluster identity.
    /// </summary>
    internal static DatabaseIdentity Cluster { get; } = new(DatabaseKind.Cluster, string.Empty, EventStoreNamespaceName.Default.Value);

    /// <summary>
    /// Creates a store identity.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <returns>The identity.</returns>
    internal static DatabaseIdentity Store(string store) => new(DatabaseKind.EventStore, store, EventStoreNamespaceName.Default.Value);

    /// <summary>
    /// Builds the original, unclipped database name.
    /// </summary>
    /// <param name="cluster">The configured cluster database.</param>
    /// <returns>The logical name.</returns>
    internal string LogicalName(string cluster) => Kind switch
    {
        DatabaseKind.Cluster => cluster,
        DatabaseKind.EventStore => $"{cluster}+es+{EventStore}",
        DatabaseKind.Namespace => $"{cluster}+es+{EventStore}+{Namespace}",
        _ => Namespace == EventStoreNamespaceName.Default.Value ? $"{cluster}+{EventStore}" : $"{cluster}+{EventStore}+{Namespace}"
    };

    /// <summary>
    /// Tests the explicit ownership order for identities in the same store.
    /// </summary>
    /// <param name="other">The competing claimant.</param>
    /// <returns>Whether this identity takes precedence.</returns>
    internal bool Outranks(DatabaseIdentity other) =>
        EventStore == other.EventStore &&
        ((Kind == DatabaseKind.EventStore && other.Kind is DatabaseKind.Namespace or DatabaseKind.ReadModels) ||
         (Kind == DatabaseKind.ReadModels && other.Kind == DatabaseKind.ReadModels && Namespace == EventStoreNamespaceName.Default.Value && Namespace != other.Namespace));
}
