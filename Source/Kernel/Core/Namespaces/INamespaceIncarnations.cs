// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Routes opt-in namespace operations to their durable owner.</summary>
public interface INamespaceIncarnations
{
    /// <summary>Gets whether the deployment has enabled incarnation coordination.</summary>
    bool Enabled { get; }

    /// <summary>Gets a fenced provider capability or refuses enablement.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The provider capability.</returns>
    IHaveNamespaceIncarnations GetStorage(EventStoreName eventStore);

    /// <summary>Ensures a namespace through its owner, including already-listed names.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The requested namespace.</param>
    /// <returns>The acknowledged canonical namespace name.</returns>
    Task<EventStoreNamespaceName> Ensure(EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>Removes a namespace through its owner.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The requested namespace.</param>
    /// <returns>Acknowledged deletion, never merely pending work.</returns>
    Task Remove(EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>Resolves stored casing without creating or admitting a namespace.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The requested namespace.</param>
    /// <returns>The stored name, if listed.</returns>
    Task<EventStoreNamespaceName?> GetCanonical(EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>Reads listed namespaces, excluding pending and terminal removal states.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The names eligible for forwarding subscriptions.</returns>
    Task<IEnumerable<EventStoreNamespaceName>> GetAll(EventStoreName eventStore);

    /// <summary>Checks a narrowly interleaved listing callback against persisted removal intent.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="incarnation">The exact closing identity.</param>
    /// <param name="operation">The exact removal operation.</param>
    /// <returns>The validation acknowledgement.</returns>
    Task ValidateForget(EventStoreName eventStore, EventStoreNamespaceName @namespace, NamespaceIncarnationId incarnation, Guid operation);
}
