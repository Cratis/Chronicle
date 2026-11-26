// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_registry_records_another_subject_for_the_serial : given.a_peer_admission
{
    async Task Because()
    {
        await _authority.EnsureAuthority();
        await SetPolicy("acme");
        await SetPolicy("globex");
        var certificate = SignedBy(_keys.Authority, "spiffe://example/customer/acme/kernel/prod");
        var now = _time.GetUtcNow();

        // The registry says this serial was issued to globex, whatever the certificate claims.
        await _registry.Append(
            certificate.SerialNumber,
            new FederationCertificateIssued("enrollment", "globex", "prod", "spiffe://example/customer/globex/kernel/prod", now.AddMinutes(-1), now.AddDays(14), FederationIssuanceReasons.Enrollment, "enrollment"));
        _admitted = await Admit(certificate);
    }

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_the_subject_does_not_match() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.SubjectMismatch);
}
