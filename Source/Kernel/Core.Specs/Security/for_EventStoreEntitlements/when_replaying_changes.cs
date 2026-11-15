// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EventStoreEntitlements;

public class when_replaying_changes : given.an_entitlements_grain
{
    StoreEntitlementsState _replayed;

    async Task Because()
    {
        await _entitlements.Grant(_grant);
        await _entitlements.Grant(_grant with { Access = StoreAccess.Read | StoreAccess.Append });
        await _entitlements.Revoke(_grant);
        _replayed = await CreateEntitlements().GetSnapshot();
    }

    [Fact] void should_replay_the_revocation() => _replayed.Grants.ShouldBeEmpty();
    [Fact] void should_advance_the_version_for_each_change() => _replayed.Version.ShouldEqual(3L);
    [Fact] void should_write_a_proof_on_every_change() => Recorded<EventStoreAccessGranted>().All(@event => !string.IsNullOrEmpty(@event.Proof)).ShouldBeTrue();
    [Fact] void should_use_the_dedicated_source() => _appended.TrueForAll(@event => @event.Context.EventSourceId == StoreEntitlementsHistory.EventSourceId).ShouldBeTrue();
    [Fact] async Task should_broadcast_each_committed_change() => await _broadcast.Received(3).Refresh(Arg.Any<StoreEntitlementsState>());
}
