// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_identity_is_in_another_trust_domain : given.a_peer_admission
{
    async Task Because()
    {
        await _authority.EnsureAuthority();
        await SetPolicy("acme");
        _admitted = await Admit(SignedBy(_keys.Authority, "spiffe://elsewhere/customer/acme/kernel/prod"));
    }

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_it_is_another_trust_domain() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.WrongTrustDomain);
}
