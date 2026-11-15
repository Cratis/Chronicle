// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The federation certificate authority was created. The event source id is the authority. The private key is never in the event.
/// </summary>
/// <param name="Fingerprint">The SHA-256 fingerprint of the authority certificate, as lowercase hex.</param>
/// <param name="TrustDomain">The trust domain the authority issues identities in.</param>
/// <param name="NotBefore">When the authority certificate becomes valid.</param>
/// <param name="NotAfter">When the authority certificate expires.</param>
[EventType]
public record FederationAuthorityCreated(string Fingerprint, string TrustDomain, DateTimeOffset NotBefore, DateTimeOffset NotAfter);
