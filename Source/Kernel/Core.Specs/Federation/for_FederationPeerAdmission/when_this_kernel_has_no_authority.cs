// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_this_kernel_has_no_authority : given.a_peer_admission
{
    async Task Because() => _admitted = await Admit(SignedBy(AnotherAuthority(), "spiffe://example/customer/acme/kernel/prod"));

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_there_is_no_authority() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.NoAuthority);
    [Fact] void should_not_create_an_authority() => _registry.Appended.ShouldBeEmpty();
}
