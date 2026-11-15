// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_admitting_a_peer_with_an_active_policy : given.a_peer_admission
{
    async Task Because()
    {
        var certificate = await Issued("acme", "prod");
        await SetPolicy("acme", FederationDirection.AcceptorOutbound, allPublic: false, "order-placed");
        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        _admitted = await Admit(certificate);
    }

    [Fact] void should_admit_the_peer() => _admitted.IsAdmitted.ShouldBeTrue();
    [Fact] void should_not_give_a_refusal() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.None);
    [Fact] void should_give_the_subject_of_the_identity() => _admitted.Subject.ShouldEqual("acme");
    [Fact] void should_give_the_instance_of_the_identity() => _admitted.Instance.ShouldEqual("prod");
    [Fact] void should_give_the_target_decided_by_the_policy() => _admitted.Policy.EntryFor(FederationDirection.AcceptorOutbound).EventStore.ShouldEqual(TargetStore);
    [Fact] void should_give_the_event_types_of_the_direction() => _admitted.Policy.EntryFor(FederationDirection.AcceptorOutbound).EventTypes.ShouldContainOnly("order-placed");
    [Fact] void should_keep_the_directions_apart() => _admitted.Policy.EntryFor(FederationDirection.AcceptorInbound).AllPublic.ShouldBeTrue();
}
