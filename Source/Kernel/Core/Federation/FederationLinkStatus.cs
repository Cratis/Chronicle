// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Producer-side operational state of a link.
/// </summary>
/// <param name="LastPull">When the peer last pulled.</param>
/// <param name="EventsDelivered">The number of events delivered.</param>
/// <param name="BytesDelivered">The number of content bytes delivered.</param>
/// <param name="Throttled">The number of pulls that were throttled.</param>
public record FederationLinkStatus(DateTimeOffset? LastPull, ulong EventsDelivered, ulong BytesDelivered, ulong Throttled);
