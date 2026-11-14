// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// An issued certificate was revoked. The event source id is the serial number. Revocation takes effect at once.
/// </summary>
/// <param name="Identity">The identity in the certificate.</param>
/// <param name="RevokedBy">The identity that revoked the certificate.</param>
[EventType]
public record FederationCertificateRevoked(string Identity, string RevokedBy);
