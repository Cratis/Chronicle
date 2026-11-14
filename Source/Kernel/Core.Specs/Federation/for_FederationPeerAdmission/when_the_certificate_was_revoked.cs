// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_certificate_was_revoked : given.a_peer_admission
{
    async Task Because()
    {
        var (issuance, key) = await Redeem(await Enroll("acme", "prod"));
        key.Dispose();
        var certificate = CertificateOf(issuance);
        await SetPolicy("acme");
        var enrollment = (await _registry.GetEnrollments()).Single();
        await _authority.RevokeEnrollment(enrollment.Id);
        _admitted = await Admit(certificate);
        certificate.Dispose();
    }

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_the_certificate_was_revoked() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.CertificateRevoked);
}
