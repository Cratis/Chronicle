// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Security;

/// <summary>Retained audit observations not covered by current explicit grants. This is evidence, not policy.</summary>
/// <param name="Holder">The issuer-qualified principal.</param>
/// <param name="EventStore">The observed ordinary store.</param>
/// <param name="Namespace">The observed namespace.</param>
/// <param name="Access">The attempted access.</param>
/// <param name="FirstSeen">The first retained occurrence.</param>
/// <param name="LastSeen">The most recent occurrence.</param>
/// <param name="Count">The retained rate-limited occurrence count.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Administrators)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record EventStoreAccessPreflight(EventStoreAccessHolder Holder, EventStoreName EventStore, EventStoreNamespaceName? Namespace, StoreAccess Access, DateTimeOffset FirstSeen, DateTimeOffset LastSeen, long Count)
{
    /// <summary>Lists uncovered evidence; never creates or reconciles grants.</summary>
    /// <param name="grains">The grain factory.</param>
    /// <returns>Uncovered observations retained since activation.</returns>
    public static async Task<IEnumerable<EventStoreAccessPreflight>> Uncovered(IGrainFactory grains)
    {
        var grants = new StoreEntitlementsSnapshot(await grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).GetSnapshot());
        var observations = await grains.GetGrain<IStoreEntitlementObservations>(IStoreEntitlementObservations.Key).GetSnapshot();
        return observations.Where(observation => !grants.Allows(observation.Holder, observation.EventStore, observation.Namespace, observation.Access))
            .Select(observation => new EventStoreAccessPreflight(new(observation.Holder.Issuer, observation.Holder.Subject, observation.Holder.IsClient), observation.EventStore, observation.Namespace, observation.Access, observation.FirstSeen, observation.LastSeen, observation.Count)).ToArray();
    }
}
