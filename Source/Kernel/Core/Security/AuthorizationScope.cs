// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents where a capability is exercised: the kernel, an event store or a namespace of an event store.
/// </summary>
/// <param name="EventStore">The event store, or null when the scope is the kernel.</param>
/// <param name="Namespace">The namespace, or null when the scope is not a namespace.</param>
public record AuthorizationScope(EventStoreName? EventStore, EventStoreNamespaceName? Namespace)
{
    /// <summary>
    /// Gets the kernel-wide scope.
    /// </summary>
    public static AuthorizationScope Kernel { get; } = new(null, null);

    /// <summary>
    /// Creates a scope for an event store.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The scope.</returns>
    public static AuthorizationScope ForEventStore(EventStoreName eventStore) => new(eventStore, null);

    /// <summary>
    /// Creates a scope for a namespace of an event store.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The scope.</returns>
    public static AuthorizationScope ForNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace) => new(eventStore, @namespace);
}
