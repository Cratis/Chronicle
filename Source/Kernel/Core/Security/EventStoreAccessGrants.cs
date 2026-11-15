// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Security;

/// <summary>A visible persisted grant, including its source.</summary>
/// <param name="Holder">The issuer-qualified principal.</param>
/// <param name="EventStore">The store, or null for all ordinary stores.</param>
/// <param name="Namespace">The namespace, or null for all namespaces.</param>
/// <param name="Access">The access flags.</param>
/// <param name="Source">The grant's source.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Administrators)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record EventStoreAccessGrants(EventStoreAccessHolder Holder, EventStoreName? EventStore, EventStoreNamespaceName? Namespace, StoreAccess Access, EventStoreAccessGrantSource Source)
{
    /// <summary>Lists explicit grants without modifying them.</summary>
    /// <param name="grains">The grain factory.</param>
    /// <returns>The authoritative grants.</returns>
    public static async Task<IEnumerable<EventStoreAccessGrants>> All(IGrainFactory grains) =>
        (await grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).GetSnapshot()).Grants.Select(grant => new EventStoreAccessGrants(new(grant.Holder.Issuer, grant.Holder.Subject, grant.Holder.IsClient), grant.EventStore, grant.Namespace, grant.Access, grant.Source));
}
