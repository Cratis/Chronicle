// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Operational state of the inbound direction of a hosted link, the events a dialer served to this kernel.
/// </summary>
/// <param name="LastReceived">When events were last received.</param>
/// <param name="EventsReceived">The number of events appended to the inbox.</param>
/// <param name="BytesReceived">The number of content bytes appended to the inbox.</param>
public record FederationInboundStatus(DateTimeOffset? LastReceived, ulong EventsReceived, ulong BytesReceived);
