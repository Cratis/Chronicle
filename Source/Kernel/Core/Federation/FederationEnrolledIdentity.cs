// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The identity this kernel holds after it enrolled with a hosted federation authority.
/// </summary>
/// <param name="Certificate">The issued certificate, with its private key.</param>
/// <param name="Authority">The authority certificate, without a private key.</param>
/// <param name="Identity">The identity in the certificate.</param>
/// <param name="Serial">The serial number of the certificate.</param>
/// <param name="NotBefore">When the certificate becomes valid.</param>
/// <param name="NotAfter">When the certificate expires.</param>
/// <param name="Endpoint">The public endpoint of the authority.</param>
/// <param name="AuthorityFingerprint">The SHA-256 fingerprint of the authority certificate, as lowercase hex.</param>
public record FederationEnrolledIdentity(
    X509Certificate2 Certificate,
    X509Certificate2 Authority,
    string Identity,
    string Serial,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Endpoint,
    string AuthorityFingerprint);
