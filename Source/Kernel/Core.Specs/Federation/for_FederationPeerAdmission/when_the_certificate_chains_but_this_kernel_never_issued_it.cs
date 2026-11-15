// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_certificate_chains_but_this_kernel_never_issued_it : given.a_peer_admission
{
    async Task Because()
    {
        await _authority.EnsureAuthority();
        await SetPolicy("acme");
        _admitted = await Admit(SignedBy(_keys.Authority, "spiffe://example/customer/acme/kernel/prod"));
    }

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_it_was_not_issued() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.NotIssued);
}
