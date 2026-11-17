// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Decides whether the certificate another silo presents on the cluster transport is acceptable.
/// </summary>
/// <remarks>
/// Silos reach each other by IP address, so the certificate name cannot be checked, and the cluster does not
/// share the operating system trust store with an internal authority. What a silo can say is "this is the
/// certificate I hold, or one issued by the authority above it". That is what is checked, and nothing else is accepted.
/// </remarks>
internal static class ClusterPeerCertificateValidator
{
    /// <summary>
    /// Determines whether the peer certificate is the local certificate or chains to an authority in the local certificate's chain.
    /// </summary>
    /// <param name="local">The certificate this silo presents.</param>
    /// <param name="peer">The certificate the other silo presented.</param>
    /// <param name="authorities">Certificates found next to the local certificate in its file, such as the issuing authority.</param>
    /// <param name="presentedChain">The chain the other silo presented, if any, used for intermediate certificates.</param>
    /// <returns>True when the peer is trusted; otherwise false.</returns>
    internal static bool IsTrusted(X509Certificate2 local, X509Certificate2? peer, X509Certificate2Collection? authorities = null, X509Chain? presentedChain = null)
    {
        if (peer is null)
        {
            return false;
        }

        if (string.Equals(peer.Thumbprint, local.Thumbprint, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        using var anchors = BuildLocalChain(local);
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        foreach (var element in anchors.ChainElements)
        {
            chain.ChainPolicy.CustomTrustStore.Add(element.Certificate);
        }

        if (authorities is not null)
        {
            chain.ChainPolicy.CustomTrustStore.AddRange(authorities);
        }

        if (presentedChain is not null)
        {
            foreach (var element in presentedChain.ChainElements)
            {
                chain.ChainPolicy.ExtraStore.Add(element.Certificate);
            }
        }

        return chain.Build(peer);
    }

    static X509Chain BuildLocalChain(X509Certificate2 local)
    {
        var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
        chain.Build(local);
        return chain;
    }
}
