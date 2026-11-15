// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Decides whether a federation server can be trusted because it chains to the authority root whose fingerprint an enrollment
/// code carries. The system trust store is not consulted.
/// </summary>
public static class FederationServerChainValidator
{
    /// <summary>
    /// Validate a presented server certificate.
    /// </summary>
    /// <param name="certificate">The certificate the server presented.</param>
    /// <param name="errors">The errors the TLS stack found. Chain errors are expected, because the root is not in the system store; a name mismatch is not.</param>
    /// <param name="authority">The authority root, whose fingerprint has already been checked against the enrollment code.</param>
    /// <param name="now">The moment to check validity at.</param>
    /// <returns>True when the server certificate is valid, is for the host and chains to the authority root.</returns>
    public static bool Validate(X509Certificate2? certificate, SslPolicyErrors errors, X509Certificate2 authority, DateTimeOffset now) =>
        certificate is not null
        && !errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch)
        && !errors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable)
        && FederationCertificateAuthority.ChainsTo(certificate, authority, now, FederationCertificateAuthority.ServerAuthentication);
}
