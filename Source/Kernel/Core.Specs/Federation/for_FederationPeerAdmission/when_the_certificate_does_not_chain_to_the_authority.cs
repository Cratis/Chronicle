// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_certificate_does_not_chain_to_the_authority : given.a_peer_admission
{
    async Task Because()
    {
        await Issued("acme", "prod");
        await SetPolicy("acme");
        _admitted = await Admit(SignedBy(AnotherAuthority(), "spiffe://example/customer/acme/kernel/prod"));
    }

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_it_does_not_chain() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.NotChained);
}
