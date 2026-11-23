// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces;

/// <summary>An authorized snapshot of an existing namespace, without creating or recreating it.</summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The canonical namespace name.</param>
/// <param name="Incarnation">The kernel-issued identity, available only while open.</param>
/// <param name="Status">The durable lifecycle status.</param>
/// <param name="ProtocolVersion">The namespace-bound append protocol version.</param>
/// <remarks>Capture before decision reads. Lookup alone does not bind existing writers or units of work.</remarks>
public record NamespaceBinding(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    NamespaceIncarnationId? Incarnation,
    NamespaceBindingStatus Status,
    int ProtocolVersion);
