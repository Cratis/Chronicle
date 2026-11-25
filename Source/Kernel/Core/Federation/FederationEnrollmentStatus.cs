// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The state of this kernel as a peer of a hosted federation authority. Neither key material nor the enrollment code is ever part of it.
/// </summary>
/// <param name="Enrolled">Whether this kernel holds a certificate from an authority.</param>
/// <param name="Identity">The identity in the certificate. Empty when not enrolled.</param>
/// <param name="Serial">The serial number of the certificate. Empty when not enrolled.</param>
/// <param name="NotAfter">When the certificate expires, or null when not enrolled.</param>
/// <param name="RenewAfter">When the certificate becomes due for renewal, or null when not enrolled.</param>
/// <param name="Endpoint">The public endpoint of the authority. Empty when not enrolled.</param>
/// <param name="AuthorityFingerprint">The SHA-256 fingerprint of the authority root, as lowercase hex. Empty when not enrolled.</param>
/// <param name="Expired">Whether the certificate has expired, so the kernel is locked out until it is enrolled again.</param>
/// <param name="Connected">Whether the dialer session to the authority is open and its last operation succeeded. False when not enrolled or expired.</param>
/// <param name="LastExchange">When a pull was last answered, an inbound batch was last delivered or a renewal last succeeded, or null when none has.</param>
/// <param name="LastError">The last session failure, scrubbed, cleared by the next successful exchange. Empty when there is none.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Federation)]
[Authorize(Policy = WellKnownAuthorizationPolicies.FederationEnrollment)]
public record FederationEnrollmentStatus(
    bool Enrolled,
    string Identity,
    string Serial,
    DateTimeOffset? NotAfter,
    DateTimeOffset? RenewAfter,
    string Endpoint,
    string AuthorityFingerprint,
    bool Expired,
    bool Connected,
    DateTimeOffset? LastExchange,
    string LastError)
{
    /// <summary>
    /// Gets the enrollment status of this kernel.
    /// </summary>
    /// <param name="registry">The <see cref="IFederationCertificateRegistry"/> holding the installed identity.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> to decide expiry with.</param>
    /// <param name="status">The <see cref="IFederationStatus"/> holding the state of the dialer session.</param>
    /// <returns>The status.</returns>
    public static async Task<FederationEnrollmentStatus> CurrentEnrollmentStatus(IFederationCertificateRegistry registry, TimeProvider timeProvider, IFederationStatus status)
    {
        var installed = await registry.GetInstalledIdentity();
        if (installed is null)
        {
            return new(false, string.Empty, string.Empty, null, null, string.Empty, string.Empty, false, false, null, string.Empty);
        }

        var expired = timeProvider.GetUtcNow() >= installed.NotAfter;
        var dial = status.GetDial();

        return new(
            true,
            installed.Identity,
            installed.Serial,
            installed.NotAfter,
            installed.NotBefore + ((installed.NotAfter - installed.NotBefore) / 2),
            installed.Endpoint,
            installed.AuthorityFingerprint,
            expired,
            dial.Connected && !expired,
            dial.LastExchange,
            dial.LastError ?? string.Empty);
    }
}
