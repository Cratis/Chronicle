// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// What has moved in one direction of the link of a hosted federation subject.
/// </summary>
/// <param name="LastActivity">When events last moved in this direction, or null when none has since the kernel started.</param>
/// <param name="Events">The number of events that moved.</param>
/// <param name="Bytes">The number of content bytes that moved.</param>
/// <param name="Lag">The number of events not yet moved, or null when the kernel does not know it for this direction.</param>
public record FederationLinkTraffic(DateTimeOffset? LastActivity, ulong Events, ulong Bytes, ulong? Lag);
