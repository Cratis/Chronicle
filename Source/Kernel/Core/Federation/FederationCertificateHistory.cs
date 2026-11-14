// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Folds the certificate authority and enrollment events, in sequence order, into the enrollments, the registry of issued
/// certificates and this kernel's own installed identity. Events that are not enrollment events are ignored.
/// </summary>
internal sealed class FederationCertificateHistory
{
    readonly Dictionary<string, FederationEnrollmentState> _enrollments = new(StringComparer.Ordinal);
    readonly Dictionary<string, FederationIssuedCertificateState> _certificates = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the authority creation event, or null when this kernel never created an authority.
    /// </summary>
    public FederationAuthorityCreated? Authority { get; private set; }

    /// <summary>
    /// Gets the identity this kernel most recently installed, or null when it is not enrolled.
    /// </summary>
    public FederationIdentityInstalled? Identity { get; private set; }

    /// <summary>
    /// Gets the enrollments, oldest first by creation.
    /// </summary>
    public IReadOnlyList<FederationEnrollmentState> Enrollments => [.. _enrollments.Values.OrderBy(_ => _.CreatedAt).ThenBy(_ => _.Id, StringComparer.Ordinal)];

    /// <summary>
    /// Gets the registry of issued certificates, revoked ones included.
    /// </summary>
    public IReadOnlyList<FederationIssuedCertificateState> Certificates => [.. _certificates.Values.OrderBy(_ => _.NotBefore).ThenBy(_ => _.Serial, StringComparer.Ordinal)];

    /// <summary>
    /// Normalize a serial number so that case and leading zeros never decide whether a certificate is found.
    /// </summary>
    /// <param name="serial">The serial number, as hex.</param>
    /// <returns>The normalized serial number.</returns>
    public static string Normalize(string serial) => serial.TrimStart('0').ToUpperInvariant();

    /// <summary>
    /// Find an enrollment.
    /// </summary>
    /// <param name="id">The enrollment id.</param>
    /// <returns>The enrollment, or null when unknown.</returns>
    public FederationEnrollmentState? FindEnrollment(string id) => _enrollments.GetValueOrDefault(id);

    /// <summary>
    /// Find an issued certificate.
    /// </summary>
    /// <param name="serial">The serial number, as hex.</param>
    /// <returns>The issued certificate, or null when this kernel never issued it.</returns>
    public FederationIssuedCertificateState? FindCertificate(string serial) => _certificates.GetValueOrDefault(Normalize(serial));

    /// <summary>
    /// Apply an event to the folded state.
    /// </summary>
    /// <param name="id">The event source id.</param>
    /// <param name="content">The deserialized event.</param>
    public void Apply(string id, object content)
    {
        switch (content)
        {
            case FederationAuthorityCreated authority:
                Authority = authority;
                break;

            case FederationEnrollmentCreated created:
                _enrollments[id] = new(id, created.Subject, created.Instance, created.TokenHash, created.CreatedAt, created.ExpiresAt, created.CreatedBy, false, false);
                break;

            case FederationEnrollmentRevoked when _enrollments.TryGetValue(id, out var revoked):
                _enrollments[id] = revoked with { Revoked = true };
                break;

            case FederationCertificateIssued issued:
                var serial = Normalize(id);
                _certificates[serial] = new(serial, issued.EnrollmentId, issued.Subject, issued.Instance, issued.Identity, issued.NotBefore, issued.NotAfter, issued.Reason, false);
                if (issued.Reason == FederationIssuanceReasons.Enrollment && _enrollments.TryGetValue(issued.EnrollmentId, out var redeemed))
                {
                    _enrollments[issued.EnrollmentId] = redeemed with { Redeemed = true };
                }

                break;

            case FederationCertificateRevoked when _certificates.TryGetValue(Normalize(id), out var certificate):
                _certificates[certificate.Serial] = certificate with { Revoked = true };
                break;

            case FederationIdentityInstalled installed:
                Identity = installed;
                break;
        }
    }
}
