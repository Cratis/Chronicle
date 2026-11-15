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
/// <param name="Id">The binding id.</param>
/// <param name="LogicalSource">The logical source name consumer code refers to.</param>
/// <param name="Endpoint">The federation endpoint of the peer.</param>
/// <param name="ServerPin">The pinned server certificate.</param>
/// <param name="ClientCertificate">The name of the client certificate presented.</param>
/// <param name="RemoteEventStore">The event store on the peer.</param>
/// <param name="RemoteNamespace">The namespace on the peer.</param>
/// <param name="TargetEventStore">The local event store whose inbox receives the events.</param>
/// <param name="TargetNamespace">The local namespace.</param>
/// <param name="Origin">Where the binding came from.</param>
/// <param name="State">The state of the link.</param>
/// <param name="LastSourceSequenceNumber">The last source sequence number appended to the inbox.</param>
/// <param name="RemoteTailSequenceNumber">The remote outbox tail last observed.</param>
/// <param name="Lag">The number of outbox sequence numbers not yet consumed.</param>
/// <param name="LastDelivery">When events were last appended.</param>
/// <param name="LastError">The last error, never containing a secret.</param>
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
    /// <param name="registry">The <see cref="IFederationRegistry"/> holding the bindings.</param>
    /// <param name="status">The <see cref="IFederationStatus"/> holding the live state of the links.</param>
    /// <returns>The bindings.</returns>
    public static async Task<IEnumerable<FederationBinding>> AllBindings(IFederationRegistry registry, IFederationStatus status)
    {
        var bindings = await registry.GetBindings();
        return bindings.Select(binding =>
        {
            var live = status.GetBinding(binding.Id);
            return new FederationBinding(
                binding.Id,
                binding.LogicalSource,
                binding.Endpoint,
                binding.ServerPin,
                binding.ClientCertificate,
                binding.RemoteEventStore,
                binding.RemoteNamespace,
                binding.TargetEventStore,
                binding.TargetNamespace,
                binding.Origin,
                live.State,
                live.LastSourceSequenceNumber,
                live.RemoteTailSequenceNumber,
                live.Lag,
                live.LastDelivery,
                live.LastError);
        }).ToArray();
    }
}
