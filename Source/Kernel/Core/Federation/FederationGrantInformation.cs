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
    /// <returns>The grants.</returns>
    public static Task<IEnumerable<FederationGrantInformation>> AllGrants() => throw new NotImplementedException();
}
