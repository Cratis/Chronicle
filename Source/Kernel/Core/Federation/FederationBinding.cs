// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A consumer-side binding and the live state of its link.
/// </summary>
[ReadModel]
[BelongsTo(WellKnownServices.Federation)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record FederationBinding(
    string Id,
    string LogicalSource,
    string Endpoint,
    string ServerPin,
    string ClientCertificate,
    string RemoteEventStore,
    string RemoteNamespace,
    string TargetEventStore,
    string TargetNamespace,
    string Origin,
    string State,
    ulong LastSourceSequenceNumber,
    ulong RemoteTailSequenceNumber,
    ulong Lag,
    DateTimeOffset? LastDelivery,
    string? LastError)
{
    /// <summary>
    /// Gets all consumer bindings.
    /// </summary>
    /// <returns>The bindings.</returns>
    public static Task<IEnumerable<FederationBinding>> AllBindings() => throw new NotImplementedException();
}
