// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Provides the TLS certificate validation shared by the client's internal HTTP calls (OAuth token
/// requests, load-balancer connection-count probes) to accept the self-signed certificate Chronicle
/// Server generates in development when no certificate is configured.
/// </summary>
internal static class DevelopmentCertificateValidation
{
    /// <summary>
    /// Determines whether a certificate that failed validation should be accepted as a development
    /// self-signed certificate.
    /// </summary>
    /// <param name="chain">The <see cref="X509Chain"/> built for the certificate.</param>
    /// <param name="sslPolicyErrors">The <see cref="SslPolicyErrors"/> reported for the certificate.</param>
    /// <returns>True if the certificate should be accepted, false if not.</returns>
    public static bool AcceptSelfSigned(X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors &&
            chain?.ChainStatus.All(status => status.Status is X509ChainStatusFlags.PartialChain or X509ChainStatusFlags.UntrustedRoot) == true)
        {
            return true;
        }

        return sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
    }
}
