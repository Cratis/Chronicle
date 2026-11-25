// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_renewing_certificates_that_must_be_refused : given.a_federation_authority
{
    FederationIssuance _revoked;
    FederationIssuance _expired;
    FederationIssuance _notInTheRegistry;
    FederationIssuance _fromAnotherAuthority;
    FederationIssuance _garbage;

    async Task Because()
    {
        using var newKey = NewKey();
        var csr = Csr(newKey);

        var revokedCode = await Enroll("acme", "revoked");
        var (revokedIssuance, revokedKey) = await Redeem(revokedCode);
        using var revokedCertificate = CertificateOf(revokedIssuance);
        revokedKey.Dispose();
        await _authority.RevokeEnrollment(revokedCode.EnrollmentId);
        _revoked = await _authority.Renew(revokedCertificate.RawData, csr);

        // Signed by the authority, as if its key had leaked, but never registered.
        using var leakedKey = NewKey();
        using var unregistered = FederationCertificateAuthority.IssueClient(
            _keys.Authority,
            Csr(leakedKey),
            "spiffe://example/customer/mallory/kernel/prod",
            _time.GetUtcNow().AddMinutes(-5),
            _time.GetUtcNow().AddDays(14));
        _notInTheRegistry = await _authority.Renew(unregistered.RawData, csr);

        using var otherAuthority = FederationCertificateAuthority.CreateAuthority("example", _time.GetUtcNow().AddDays(-1), _time.GetUtcNow().AddYears(1));
        using var otherKey = NewKey();
        using var forged = FederationCertificateAuthority.IssueClient(
            otherAuthority,
            Csr(otherKey),
            "spiffe://example/customer/acme/kernel/prod",
            _time.GetUtcNow().AddMinutes(-5),
            _time.GetUtcNow().AddDays(14));
        _fromAnotherAuthority = await _authority.Renew(forged.RawData, csr);

        var expiredCode = await Enroll("acme", "expired");
        var (expiredIssuance, expiredKey) = await Redeem(expiredCode);
        using var expiredCertificate = CertificateOf(expiredIssuance);
        expiredKey.Dispose();
        _time.Advance(TimeSpan.FromDays(15));
        _expired = await _authority.Renew(expiredCertificate.RawData, csr);

        _garbage = await _authority.Renew([1, 2, 3], csr);
    }

    [Fact] void should_refuse_a_revoked_certificate() => _revoked.ShouldEqual(FederationIssuance.Rejected);
    [Fact] void should_refuse_a_certificate_the_registry_never_issued() => _notInTheRegistry.ShouldEqual(FederationIssuance.Rejected);
    [Fact] void should_refuse_a_certificate_from_another_authority() => _fromAnotherAuthority.ShouldEqual(FederationIssuance.Rejected);
    [Fact] void should_refuse_an_expired_certificate_so_a_peer_that_stopped_renewing_is_locked_out() => _expired.ShouldEqual(FederationIssuance.Rejected);
    [Fact] void should_refuse_something_that_is_not_a_certificate() => _garbage.ShouldEqual(FederationIssuance.Rejected);
}
