// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security;

/// <summary>Records an explicit event store access grant.</summary>
/// <param name="Holder">The receiving principal.</param>
/// <param name="Access">The granted access.</param>
/// <param name="Source">The authority supplying the grant.</param>
/// <param name="EventStore">The store, or null for all non-System stores.</param>
/// <param name="Namespace">The namespace, or null for all namespaces.</param>
/// <param name="Proof">The kernel proof.</param>
[EventType]
public record EventStoreAccessGranted(PrincipalKey Holder, StoreAccess Access, EventStoreAccessGrantSource Source, EventStoreName? EventStore = null, EventStoreNamespaceName? Namespace = null, string Proof = "");
