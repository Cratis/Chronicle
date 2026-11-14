// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_subject_has_no_policy : given.a_peer_admission
{
    async Task Because()
    {
        var certificate = await Issued("acme", "prod");
        await SetPolicy("globex");
        _admitted = await Admit(certificate);
    }

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_there_is_no_policy() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.NoPolicy);
}
