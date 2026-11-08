// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Producer-side operational state of a link.
/// </summary>
public record FederationLinkStatus(DateTimeOffset? LastPull, ulong EventsDelivered, ulong BytesDelivered, ulong Throttled);
