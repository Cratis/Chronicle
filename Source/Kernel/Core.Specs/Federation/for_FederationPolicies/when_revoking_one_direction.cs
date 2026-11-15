// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_revoking_one_direction : given.a_peer_admission
{
    FederationPolicyState _both;
    FederationPolicyState _afterRevoke;
    int _eventsBeforeRepeat;
    int _eventsAfterRepeat;

    async Task Because()
    {
        await _policies.Set("acme", FederationDirection.AcceptorOutbound, "store", "ns", true, [], "alice");
        await _policies.Set("acme", FederationDirection.AcceptorInbound, "other-store", "other-ns", true, [], "alice");
        _both = await _registry.FindPolicy("acme");
        await _policies.RevokeDirection("acme", FederationDirection.AcceptorInbound, "bob");
        _afterRevoke = await _registry.FindPolicy("acme");
        _eventsBeforeRepeat = _registry.Appended.Count;
        await _policies.RevokeDirection("acme", FederationDirection.AcceptorInbound, "bob");
        _eventsAfterRepeat = _registry.Appended.Count;
    }

    [Fact] void should_have_held_both_directions_before() => _both.Entries.Count.ShouldEqual(2);
    [Fact] void should_remove_the_revoked_direction() => _afterRevoke.EntryFor(FederationDirection.AcceptorInbound).ShouldBeNull();
    [Fact] void should_keep_the_other_direction() => _afterRevoke.EntryFor(FederationDirection.AcceptorOutbound).EventStore.ShouldEqual("store");
    [Fact] void should_keep_the_policy_active() => _afterRevoke.Status.ShouldEqual(FederationPolicyStatus.Active);
    [Fact] void should_record_who_revoked_it() => _afterRevoke.ChangedBy.ShouldEqual("bob");
    [Fact] void should_change_nothing_when_the_direction_is_already_revoked() => _eventsAfterRepeat.ShouldEqual(_eventsBeforeRepeat);
}
