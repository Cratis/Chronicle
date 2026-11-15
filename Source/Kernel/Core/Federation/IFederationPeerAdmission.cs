// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the admission of a hosted federation peer by its certificate, its place in the registry of issued certificates
/// and the policy of its subject.
/// </summary>
public interface IFederationPeerAdmission
{
    /// <summary>
    /// Decide whether the holder of a client certificate is admitted.
    /// </summary>
    /// <remarks>
    /// The certificate must chain to the federation authority, carry exactly the identity format
    /// <c language="text">spiffe://&lt;trust-domain&gt;/customer/&lt;subject&gt;/kernel/&lt;instance&gt;</c>, have its serial number in the registry of certificates
    /// this kernel issued with the same identity, and its subject must have an active policy. Every decision reads the
    /// current state, so a suspension or revocation takes effect at once.
    /// </remarks>
    /// <param name="certificate">The presented client certificate, if any.</param>
    /// <returns>The admission.</returns>
    Task<FederationAdmission> Admit(X509Certificate2? certificate);
}
