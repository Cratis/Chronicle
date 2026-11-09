// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Authenticates a connecting peer by matching its client certificate against the pinned certificates of the known links.
/// </summary>
/// <remarks>
/// Identity comes only from the pin. The certificate chain and any shared certificate authority are not trusted, and a
/// connection matching no pin, or only a revoked link, is rejected. With no links configured every connection is rejected.
/// </remarks>
/// <param name="links">The known links.</param>
public class FederationPeerAuthenticator(IEnumerable<FederationLink> links)
{
    readonly FederationLink[] _links = [.. links];

    /// <summary>
    /// Authenticate a presented client certificate.
    /// </summary>
    /// <param name="certificate">The presented certificate, if any.</param>
    /// <returns>The link it belongs to, or null when the peer is not authenticated.</returns>
    public FederationLink? Authenticate(X509Certificate2? certificate)
    {
        if (certificate is null)
        {
            return null;
        }

        FederationLink? match = null;

        // Compare against every link so timing does not reveal which pins exist.
        foreach (var link in _links)
        {
            if (link.Pin.Matches(certificate) && !link.Revoked && match is null)
            {
                match = link;
            }
        }

        return match;
    }
}
