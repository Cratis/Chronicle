// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationPeerEnrollment;

public class when_renewal_returns_a_certificate_for_another_identity : given.a_peer_enrollment
{
    FederationEnrolledIdentity _before;
    FederationEnrolledIdentity _after;
    Exception _refused;

    async Task Because()
    {
        await _peer.Redeem((await _authority.CreateEnrollment("acme", "prod")).Code);
        _before = await _identities.Get();
        _time.Advance(TimeSpan.FromDays(8));

        using var peer = new FederationPeerEnrollment(_identities, new ForeignIdentityTransport(_transport, _authority), _time, _logger);
        _refused = await Catch.Exception(() => peer.RenewIfDue());
        _after = await _identities.Get();
    }

    [Fact] void should_refuse_the_certificate() => _refused.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_never_have_installed_it() => string.Join(',', _identities.Reasons).ShouldEqual(FederationIssuanceReasons.Enrollment);
    [Fact] void should_keep_the_identity_it_had() => _after.Serial.ShouldEqual(_before.Serial);

    sealed class ForeignIdentityTransport(IFederationEnrollmentTransport inner, IFederationAuthority issuer) : IFederationEnrollmentTransport
    {
        public Task<X509Certificate2?> GetAuthority(Uri endpoint, string authorityFingerprint, CancellationToken cancellationToken) =>
            inner.GetAuthority(endpoint, authorityFingerprint, cancellationToken);

        public Task<FederationIssuance> Redeem(Uri endpoint, X509Certificate2 authority, FederationRedeemRequest request, CancellationToken cancellationToken) =>
            inner.Redeem(endpoint, authority, request, cancellationToken);

        public async Task<FederationIssuance> Renew(Uri endpoint, X509Certificate2 authority, X509Certificate2 clientCertificate, FederationRenewRequest request, CancellationToken cancellationToken)
        {
            // A certificate the authority really signed, for the key the peer sent, but for somebody else.
            var created = await issuer.CreateEnrollment("someone-else", "prod");
            FederationEnrollmentCode.TryParse(created.Code, out var code).ShouldBeTrue();
            return await issuer.Redeem(code.EnrollmentId, code.Secret, Convert.FromBase64String(request.Csr));
        }
    }
}
