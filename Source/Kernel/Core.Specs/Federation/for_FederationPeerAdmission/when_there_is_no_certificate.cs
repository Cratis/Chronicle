// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_there_is_no_certificate : given.a_peer_admission
{
    async Task Because() => _admitted = await Admit(null);

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_there_is_no_certificate() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.NoCertificate);
}
