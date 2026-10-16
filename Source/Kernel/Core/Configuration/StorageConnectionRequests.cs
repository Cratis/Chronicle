// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Creates the <see cref="ConnectionRequest"/> instances Chronicle resolves its storage connections with.
/// </summary>
/// <remarks>
/// The scope carries the kind of database ("cluster", "eventStore", "namespace" or "readModels"), the event store and
/// the namespace, under the keys <see cref="Kind"/>, <see cref="EventStore"/> and <see cref="Namespace"/>. These are also
/// the placeholders a template can use. The cluster has an empty event store and namespace, and an event store has an
/// empty namespace.
/// </remarks>
public static class StorageConnectionRequests
{
    /// <summary>
    /// The scope key for the <see cref="StorageConnectionKind"/>.
    /// </summary>
    public const string Kind = "kind";

    /// <summary>
    /// The scope key for the event store.
    /// </summary>
    public const string EventStore = "eventStore";

    /// <summary>
    /// The scope key for the namespace.
    /// </summary>
    public const string Namespace = "namespace";

    /// <summary>
    /// Create a <see cref="ConnectionRequest"/>.
    /// </summary>
    /// <param name="storage">The kind of storage, one of <see cref="WellKnownConnectionStorage"/>.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <param name="kind">The <see cref="StorageConnectionKind"/>.</param>
    /// <param name="eventStore">The event store, empty for the cluster.</param>
    /// <param name="namespace">The namespace, empty for the cluster and an event store.</param>
    /// <returns>A new <see cref="ConnectionRequest"/>.</returns>
    public static ConnectionRequest For(string storage, ConnectionPurpose purpose, StorageConnectionKind kind, string eventStore = "", string @namespace = "") =>
        new(
            storage,
            purpose,
            new Dictionary<string, string>
            {
                [Kind] = KindName(kind),
                [EventStore] = eventStore,
                [Namespace] = @namespace
            });

    /// <summary>
    /// Get the name a <see cref="StorageConnectionKind"/> has in a request.
    /// </summary>
    /// <param name="kind">The <see cref="StorageConnectionKind"/>.</param>
    /// <returns>The name, in camel case.</returns>
    public static string KindName(StorageConnectionKind kind) => kind switch
    {
        StorageConnectionKind.Cluster => "cluster",
        StorageConnectionKind.EventStore => "eventStore",
        StorageConnectionKind.Namespace => "namespace",
        _ => "readModels"
    };
}
