// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the grain that is the federation certificate authority. The key is always <see cref="Key"/>, so there is exactly one
/// activation in the cluster, which is what makes an enrollment single-use however many silos receive requests.
/// </summary>
[Version(1)]
public interface IFederationAuthority : IGrainWithStringKey
{
    /// <summary>
    /// The grain key.
    /// </summary>
    const string Key = "default";

    /// <summary>
    /// Create an enrollment for a subject.
    /// </summary>
    /// <param name="subject">The opaque subject the enrollment is for.</param>
    /// <param name="instance">The kernel instance the identity is issued for. Empty to generate one.</param>
    /// <returns>The enrollment and the code to hand to the customer.</returns>
    /// <exception cref="InvalidFederationEnrollment">Thrown when hosted federation is not enabled or configured, or the subject or instance is not valid.</exception>
    Task<FederationEnrollmentCreatedResult> CreateEnrollment(string subject, string instance);

    /// <summary>
    /// Revoke an enrollment and every certificate issued from it. Idempotent.
    /// </summary>
    /// <param name="enrollmentId">The enrollment id.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidFederationEnrollment">Thrown when the enrollment is unknown.</exception>
    Task RevokeEnrollment(string enrollmentId);

    /// <summary>
    /// Redeem an enrollment for a certificate.
    /// </summary>
    /// <param name="enrollmentId">The enrollment id from the code.</param>
    /// <param name="secret">The token secret from the code.</param>
    /// <param name="certificateRequest">The PKCS#10 certificate request. Only its public key is used.</param>
    /// <returns>The issuance.</returns>
    Task<FederationIssuance> Redeem(string enrollmentId, string secret, byte[] certificateRequest);

    /// <summary>
    /// Renew a certificate with a new key.
    /// </summary>
    /// <param name="presentedCertificate">The DER encoded certificate the peer presented over mutual TLS.</param>
    /// <param name="certificateRequest">The PKCS#10 certificate request for the new key.</param>
    /// <returns>The issuance.</returns>
    Task<FederationIssuance> Renew(byte[] presentedCertificate, byte[] certificateRequest);

    /// <summary>
    /// Get the SHA-256 fingerprint of the authority certificate, creating the authority when none exists.
    /// </summary>
    /// <returns>The fingerprint, as lowercase hex.</returns>
    Task<string> EnsureAuthority();
}
