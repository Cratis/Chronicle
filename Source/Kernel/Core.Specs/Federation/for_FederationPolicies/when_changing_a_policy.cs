// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_changing_a_policy : given.a_peer_admission
{
    FederationPolicyState _afterSet;
    FederationPolicyState _afterSecondDirection;
    FederationPolicyState _afterSuspend;
    FederationPolicyState _afterResume;
    FederationPolicyState _afterRevoke;
    int _eventsBeforeRepeats;

    async Task Because()
    {
        await _policies.Set("acme", FederationDirection.AcceptorOutbound, "store", "ns", false, ["b", "a", "a"], "alice");
        _afterSet = await _registry.FindPolicy("acme");
        await _policies.Set("acme", FederationDirection.AcceptorInbound, "other-store", "other-ns", true, [], "bob");
        _afterSecondDirection = await _registry.FindPolicy("acme");
        await _policies.Suspend("acme", "carol");
        _afterSuspend = await _registry.FindPolicy("acme");
        _eventsBeforeRepeats = _registry.Appended.Count;
        await _policies.Suspend("acme", "carol");
        await _policies.Resume("acme", "dave");
        _afterResume = await _registry.FindPolicy("acme");
        await _policies.Resume("acme", "dave");
        await _policies.Revoke("acme", "erin");
        await _policies.Revoke("acme", "erin");
        _afterRevoke = await _registry.FindPolicy("acme");
    }

    [Fact] void should_start_active() => _afterSet.Status.ShouldEqual(FederationPolicyStatus.Active);
    [Fact] void should_record_who_set_it() => _afterSet.ChangedBy.ShouldEqual("alice");
    [Fact] void should_record_the_event_types_once_and_in_order() => string.Join(',', _afterSet.EntryFor(FederationDirection.AcceptorOutbound).EventTypes).ShouldEqual("a,b");
    [Fact] void should_keep_the_first_direction_when_the_second_is_set() => _afterSecondDirection.EntryFor(FederationDirection.AcceptorOutbound).EventStore.ShouldEqual("store");
    [Fact] void should_hold_a_target_per_direction() => _afterSecondDirection.EntryFor(FederationDirection.AcceptorInbound).EventStore.ShouldEqual("other-store");
    [Fact] void should_suspend() => _afterSuspend.Status.ShouldEqual(FederationPolicyStatus.Suspended);
    [Fact] void should_resume() => _afterResume.Status.ShouldEqual(FederationPolicyStatus.Active);
    [Fact] void should_revoke() => _afterRevoke.Status.ShouldEqual(FederationPolicyStatus.Revoked);
    [Fact] void should_record_who_revoked() => _afterRevoke.ChangedBy.ShouldEqual("erin");
    [Fact] void should_record_each_change_only_once() => _registry.Appended.Count.ShouldEqual(_eventsBeforeRepeats + 2);
    [Fact] void should_record_the_acting_identity_in_the_events() => _registry.Appended.Select(_ => _.Event).OfType<FederationPolicySuspended>().Single().SuspendedBy.ShouldEqual("carol");
}
