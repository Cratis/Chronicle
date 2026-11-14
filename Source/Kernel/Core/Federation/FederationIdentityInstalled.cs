// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// This kernel was enrolled, or renewed its certificate. The event source id is the identity slot. The private key is only ever recorded protected by the key custody.
/// </summary>
/// <param name="Identity">The identity in the certificate.</param>
/// <param name="Serial">The serial number of the certificate.</param>
/// <param name="NotBefore">When the certificate becomes valid.</param>
/// <param name="NotAfter">When the certificate expires.</param>
/// <param name="Endpoint">The public endpoint of the authority.</param>
/// <param name="AuthorityFingerprint">The SHA-256 fingerprint of the authority certificate, as lowercase hex.</param>
/// <param name="Authority">The authority certificate, DER encoded and base64.</param>
/// <param name="Certificate">The issued certificate, DER encoded and base64.</param>
/// <param name="ProtectedPrivateKey">The private key, encrypted with the secret held by the key custody, base64.</param>
/// <param name="Reason">Why it was installed, <c language="text">enrollment</c> or <c language="text">renewal</c>.</param>
[EventType]
public record FederationIdentityInstalled(
    string Identity,
    string Serial,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Endpoint,
    string AuthorityFingerprint,
    string Authority,
    string Certificate,
    string ProtectedPrivateKey,
    string Reason);
