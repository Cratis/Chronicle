// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Describes an existing namespace without creating it or refreshing a writer's binding.</summary>
/// <param name="Namespace">The canonical namespace name.</param>
/// <param name="Incarnation">The installed identity, issued only while the lifecycle is open.</param>
/// <param name="Status">The current lifecycle status.</param>
/// <param name="ProtocolVersion">The supported namespace-bound append protocol version.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Namespaces)]
public record NamespaceBinding(EventStoreNamespaceName Namespace, NamespaceIncarnationId? Incarnation, NamespaceLifecycleStatus Status, int ProtocolVersion)
{
    /// <summary>Looks up a namespace binding after store and namespace read authorization.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The requested namespace, matched case-insensitively.</param>
    /// <param name="incarnations">The opt-in incarnation capability.</param>
    /// <returns>The existing lifecycle, with an identity only when open. Lookup never calls Ensure.</returns>
    /// <exception cref="NamespaceIncarnationUnavailable">The feature or provider is unavailable, or no lifecycle has been initialized.</exception>
    public static async Task<NamespaceBinding> LookupNamespaceBinding(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        INamespaceIncarnations incarnations)
    {
        var capability = incarnations.GetStorage(eventStore);
        var canonical = await incarnations.GetCanonical(eventStore, @namespace) ?? @namespace;
        var lifecycle = await capability.NamespaceLifecycles.Get(canonical) ?? throw new NamespaceIncarnationUnavailable(eventStore);

        return new(lifecycle.Namespace, lifecycle.Status == NamespaceLifecycleStatus.Open ? lifecycle.Incarnation : null, lifecycle.Status, 1);
    }
}
