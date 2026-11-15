// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security;

/// <summary>An explicit event store grant. A null store or namespace covers all ordinary stores or namespaces.</summary>
/// <param name="Holder">The issuer-qualified principal.</param>
/// <param name="EventStore">The store, or all non-System stores.</param>
/// <param name="Namespace">The namespace, or all namespaces.</param>
/// <param name="Access">The granted access.</param>
/// <param name="Source">The authority supplying the grant.</param>
[GenerateSerializer]
public record EventStoreAccessGrant(
    [property: Id(0)] PrincipalKey Holder,
    [property: Id(1)] EventStoreName? EventStore,
    [property: Id(2)] EventStoreNamespaceName? Namespace,
    [property: Id(3)] StoreAccess Access,
    [property: Id(4)] EventStoreAccessGrantSource Source);
