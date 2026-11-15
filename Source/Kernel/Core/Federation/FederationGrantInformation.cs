// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A producer-side grant and the live state of its link.
/// </summary>
/// <param name="Id">The link id.</param>
/// <param name="ClientPin">The pinned client certificate of the peer.</param>
/// <param name="EventStore">The local event store whose outbox the peer may read.</param>
/// <param name="Namespaces">The namespaces the peer may read.</param>
/// <param name="AllPublic">Whether the grant covers all public event types.</param>
/// <param name="EventTypes">The specific event types granted when <paramref name="AllPublic"/> is false.</param>
/// <param name="Revoked">Whether the link is revoked.</param>
/// <param name="Origin">Where the grant came from.</param>
/// <param name="LastPull">When the peer last pulled.</param>
/// <param name="EventsDelivered">The number of events delivered.</param>
/// <param name="BytesDelivered">The number of content bytes delivered.</param>
/// <param name="Throttled">The number of pulls that were throttled.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Federation)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record FederationGrantInformation(
    string Id,
    string ClientPin,
    string EventStore,
    IEnumerable<string> Namespaces,
    bool AllPublic,
    IEnumerable<string> EventTypes,
    bool Revoked,
    string Origin,
    DateTimeOffset? LastPull,
    ulong EventsDelivered,
    ulong BytesDelivered,
    ulong Throttled)
{
    /// <summary>
    /// Gets all producer grants.
    /// </summary>
    /// <param name="registry">The <see cref="IFederationRegistry"/> holding the grants.</param>
    /// <param name="status">The <see cref="IFederationStatus"/> holding the live state of the links.</param>
    /// <returns>The grants.</returns>
    public static async Task<IEnumerable<FederationGrantInformation>> AllGrants(IFederationRegistry registry, IFederationStatus status)
    {
        var grants = await registry.GetGrants();
        return grants.Select(grant =>
        {
            var live = status.GetLink(grant.Id);
            return new FederationGrantInformation(
                grant.Id,
                grant.ClientPin,
                grant.EventStore,
                grant.Namespaces,
                grant.AllPublic,
                grant.EventTypes,
                grant.Revoked,
                grant.Origin,
                live.LastPull,
                live.EventsDelivered,
                live.BytesDelivered,
                live.Throttled);
        }).ToArray();
    }
}
