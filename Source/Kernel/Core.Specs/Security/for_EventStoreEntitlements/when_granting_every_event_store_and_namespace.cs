// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EventStoreEntitlements;

public class when_granting_every_event_store_and_namespace : given.an_entitlements_grain
{
    EventStoreAccessGrant _wildcard;
    StoreEntitlementsSnapshot _snapshot;
    EventStoreAccessGranted _recorded;

    async Task Because()
    {
        _wildcard = _grant with { EventStore = null, Namespace = null, Access = StoreAccess.Read | StoreAccess.Append };
        await _entitlements.Grant(_wildcard);
        _recorded = Recorded<EventStoreAccessGranted>().Single();
        _snapshot = new(await CreateEntitlements().GetSnapshot());
    }

    [Fact] void should_append_the_grant_without_a_store() => _recorded.EventStore.ShouldBeNull();
    [Fact] void should_append_the_grant_without_a_namespace() => _recorded.Namespace.ShouldBeNull();
    [Fact] void should_sign_the_grant() => _recorded.Proof.ShouldNotBeEmpty();
    [Fact] void should_replay_the_wildcard_grant() => _snapshot.Allows(_wildcard.Holder, "shop", "tenant", StoreAccess.Read | StoreAccess.Append).ShouldBeTrue();
    [Fact] void should_cover_any_other_store_and_namespace() => _snapshot.Allows(_wildcard.Holder, "other", null, StoreAccess.Append).ShouldBeTrue();
    [Fact] void should_not_cover_more_access_than_granted() => _snapshot.Allows(_wildcard.Holder, "shop", null, StoreAccess.Manage).ShouldBeFalse();
    [Fact] void should_not_cover_the_system_store() => _snapshot.Allows(_wildcard.Holder, "System", null, StoreAccess.Read).ShouldBeFalse();
}
