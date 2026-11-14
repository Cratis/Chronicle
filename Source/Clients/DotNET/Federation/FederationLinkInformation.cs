// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The live state of the link of a hosted federation subject.
/// </summary>
/// <remarks>
/// The state is operational and kept in memory per kernel node, so it starts empty when a node restarts and shows what the node that
/// answered has seen.
/// </remarks>
/// <param name="Subject">The opaque subject.</param>
/// <param name="Connected">Whether a dialer session of the subject is open now.</param>
/// <param name="LastActivity">When events last moved in either direction, or null when none has since the kernel started.</param>
/// <param name="Outbound">What the dialer pulled from this kernel, <see cref="FederationDirection.AcceptorOutbound"/>. Its lag is not known.</param>
/// <param name="Inbound">What the dialer served to this kernel, <see cref="FederationDirection.AcceptorInbound"/>.</param>
/// <param name="Throttled">The number of outbound pulls that were throttled.</param>
/// <param name="InboundState">The state of the inbound direction, such as Streaming, Idle or Backoff. Empty when nothing has been recorded.</param>
/// <param name="LastError">The last error of the inbound direction. Empty when there is none.</param>
public record FederationLinkInformation(
    string Subject,
    bool Connected,
    DateTimeOffset? LastActivity,
    FederationLinkTraffic Outbound,
    FederationLinkTraffic Inbound,
    ulong Throttled,
    string InboundState,
    string LastError);
