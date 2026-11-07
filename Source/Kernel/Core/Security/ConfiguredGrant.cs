// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security;

/// <summary>An explicit grant resolved from server configuration.</summary>
/// <param name="Holder">The issuer-qualified principal.</param>
/// <param name="EventStore">The store, or all non-System stores.</param>
/// <param name="Namespace">The namespace, or all namespaces.</param>
/// <param name="Access">The granted access.</param>
[GenerateSerializer]
public record ConfiguredGrant(
    [property: Id(0)] PrincipalKey Holder,
    [property: Id(1)] EventStoreName? EventStore,
    [property: Id(2)] EventStoreNamespaceName? Namespace,
    [property: Id(3)] StoreAccess Access);
