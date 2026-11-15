// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// An enrollment was redeemed. The event source id is the enrollment id. At most one of these can ever exist per enrollment: the event log enforces it
/// with a unique event type constraint, so that a code is single use no matter how many silos or concurrent requests redeem it.
/// </summary>
/// <param name="Serial">The serial number of the certificate issued for the redemption.</param>
[EventType]
public record FederationEnrollmentRedeemed(string Serial);
