// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Creates the certificate chain policy used only for the emulator's HTTP client.
/// </summary>
static class EmulatorCertificateValidation
{
    /// <summary>
    /// Creates a policy trusting only the emulator certificate without changing the host's trust store.
    /// </summary>
    /// <param name="trustedRoot">The emulator's generated certificate.</param>
    /// <returns>The custom-root chain policy, retaining certificate validity and server authentication checks.</returns>
    internal static X509ChainPolicy CreatePolicy(X509Certificate2 trustedRoot)
    {
        var policy = new X509ChainPolicy
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust,
            RevocationMode = X509RevocationMode.NoCheck
        };
        policy.CustomTrustStore.Add(trustedRoot);
        policy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
        return policy;
    }
}
