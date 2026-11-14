// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The runtime source of truth for the federation certificate authority: the enrollments, the registry of every certificate
/// it issued and this kernel's own installed identity. State is folded from the events in the system event log, which is also
/// the audit trail of every issuance and revocation.
/// </summary>
public interface IFederationCertificateRegistry
{
    /// <summary>
    /// Get the enrollments, redeemed and revoked ones included.
    /// </summary>
    /// <returns>The enrollments.</returns>
    Task<IReadOnlyList<FederationEnrollmentState>> GetEnrollments();

    /// <summary>
    /// Find an enrollment.
    /// </summary>
    /// <param name="id">The enrollment id.</param>
    /// <returns>The enrollment, or null when unknown.</returns>
    Task<FederationEnrollmentState?> FindEnrollment(string id);

    /// <summary>
    /// Get the registry of issued certificates, revoked ones included.
    /// </summary>
    /// <returns>The issued certificates.</returns>
    Task<IReadOnlyList<FederationIssuedCertificateState>> GetIssuedCertificates();

    /// <summary>
    /// Find an issued certificate by serial number. A certificate this kernel never issued is not found, however well it is signed.
    /// </summary>
    /// <param name="serial">The serial number, as hex.</param>
    /// <returns>The issued certificate, or null when this kernel did not issue it.</returns>
    Task<FederationIssuedCertificateState?> FindIssuedCertificate(string serial);

    /// <summary>
    /// Get the policies of every subject, revoked and suspended ones included.
    /// </summary>
    /// <returns>The policies.</returns>
    Task<IReadOnlyList<FederationPolicyState>> GetPolicies();

    /// <summary>
    /// Find the policy of a subject.
    /// </summary>
    /// <param name="subject">The opaque subject.</param>
    /// <returns>The policy, or null when the subject has none.</returns>
    Task<FederationPolicyState?> FindPolicy(string subject);

    /// <summary>
    /// Get the creation event of the authority.
    /// </summary>
    /// <returns>The event, or null when this kernel never created an authority.</returns>
    Task<FederationAuthorityCreated?> GetAuthority();

    /// <summary>
    /// Get the identity this kernel most recently installed.
    /// </summary>
    /// <returns>The identity, or null when this kernel is not enrolled.</returns>
    Task<FederationIdentityInstalled?> GetInstalledIdentity();

    /// <summary>
    /// Append an enrollment or certificate event to the system event log.
    /// </summary>
    /// <param name="eventSourceId">The event source id: the authority, an enrollment id, a serial number or the identity slot.</param>
    /// <param name="event">The event.</param>
    /// <returns>Awaitable task.</returns>
    Task Append(string eventSourceId, object @event);

    /// <summary>
    /// Claim the single redemption an enrollment has. The event log enforces it with a unique event type constraint, so it holds across silos and concurrent requests.
    /// </summary>
    /// <param name="enrollmentId">The enrollment being redeemed.</param>
    /// <param name="serial">The serial number of the certificate issued for the redemption.</param>
    /// <returns>True when this caller holds the redemption, false when the enrollment was already redeemed.</returns>
    Task<bool> ClaimRedemption(string enrollmentId, string serial);
}
