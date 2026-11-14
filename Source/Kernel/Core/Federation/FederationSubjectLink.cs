// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The live state of the link of a hosted federation subject: whether its dialer has a session open, and what has moved in each direction.
/// </summary>
/// <remarks>
/// The state is operational and kept in memory per silo, so it starts empty when a silo restarts and shows what the silo answering the
/// query has seen. Lag is only known for the inbound direction, where this kernel appends to its own inbox.
/// </remarks>
/// <param name="Subject">The opaque subject.</param>
/// <param name="Connected">Whether a dialer session of the subject is open now.</param>
/// <param name="LastActivity">When events last moved in either direction.</param>
/// <param name="OutboundLastPull">When the dialer last pulled events from this kernel.</param>
/// <param name="OutboundEvents">The number of events delivered to the dialer.</param>
/// <param name="OutboundBytes">The number of content bytes delivered to the dialer.</param>
/// <param name="OutboundThrottled">The number of pulls that were throttled.</param>
/// <param name="InboundState">The state of the inbound direction, empty when nothing has been recorded.</param>
/// <param name="InboundLastReceived">When the dialer last served events to this kernel.</param>
/// <param name="InboundEvents">The number of events received from the dialer.</param>
/// <param name="InboundBytes">The number of content bytes received from the dialer.</param>
/// <param name="InboundLag">The number of events the dialer has that are not yet received.</param>
/// <param name="LastError">The last error of the inbound direction, empty when there is none.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Federation)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record FederationSubjectLink(
    string Subject,
    bool Connected,
    DateTimeOffset? LastActivity,
    DateTimeOffset? OutboundLastPull,
    ulong OutboundEvents,
    ulong OutboundBytes,
    ulong OutboundThrottled,
    string InboundState,
    DateTimeOffset? InboundLastReceived,
    ulong InboundEvents,
    ulong InboundBytes,
    ulong InboundLag,
    string LastError)
{
    /// <summary>
    /// Gets the link state of every subject that has a policy.
    /// </summary>
    /// <param name="registry">The <see cref="IFederationCertificateRegistry"/> holding the policies.</param>
    /// <param name="status">The <see cref="IFederationStatus"/> holding the live state of the links.</param>
    /// <returns>The link state of each subject.</returns>
    public static async Task<IEnumerable<FederationSubjectLink>> AllSubjectLinks(IFederationCertificateRegistry registry, IFederationStatus status) =>
        (await registry.GetPolicies()).Select(policy => FederationSubjectLinkConverters.ToSubjectLink(policy.Subject, status)).ToArray();
}
