// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_enrollment_was_revoked_but_the_certificate_was_not : given.a_peer_admission
{
    async Task Because()
    {
        var (issuance, key) = await Redeem(await Enroll("acme", "prod"));
        key.Dispose();
        var certificate = CertificateOf(issuance);
        await SetPolicy("acme");

        // A revocation that landed between the claim of the redemption and the recording of its certificate leaves exactly this.
        var enrollment = (await _registry.GetEnrollments()).Single();
        await _registry.Append(enrollment.Id, new FederationEnrollmentRevoked("operator"));

        _admitted = await Admit(certificate);
        certificate.Dispose();
    }

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_the_enrollment_was_revoked() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.EnrollmentRevoked);
}
