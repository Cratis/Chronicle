// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security;

/// <summary>Bounded operational evidence of an access attempt, never an entitlement.</summary>
/// <param name="Holder">The issuer-qualified principal.</param>
/// <param name="EventStore">The ordinary event store.</param>
/// <param name="Namespace">The observed namespace, if present.</param>
/// <param name="Access">The attempted access.</param>
/// <param name="FirstSeen">The first retained occurrence.</param>
/// <param name="LastSeen">The most recent occurrence.</param>
/// <param name="Count">The number of rate-limited observations retained.</param>
[GenerateSerializer]
public record StoreEntitlementObservation(
    [property: Id(0)] PrincipalKey Holder,
    [property: Id(1)] EventStoreName EventStore,
    [property: Id(2)] EventStoreNamespaceName? Namespace,
    [property: Id(3)] StoreAccess Access,
    [property: Id(4)] DateTimeOffset FirstSeen,
    [property: Id(5)] DateTimeOffset LastSeen,
    [property: Id(6)] long Count);
