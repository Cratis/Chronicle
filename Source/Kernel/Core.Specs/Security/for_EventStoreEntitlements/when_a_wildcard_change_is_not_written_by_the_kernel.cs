// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security.for_EventStoreEntitlements;

/// <summary>
/// A wildcard (null store and namespace) change that the kernel did not sign must never become an entitlement.
/// Public appends cannot reach the System store (AppendRules.NotSystemEventStore), and the history only replays
/// kernel-proven changes from the System event log.
/// </summary>
public class when_a_wildcard_change_is_not_written_by_the_kernel : given.an_entitlements_grain
{
    static readonly PrincipalKey _attacker = new(PrincipalKey.LocalIssuer, "attacker", true);
    const StoreAccess Everything = StoreAccess.Read | StoreAccess.Append | StoreAccess.Manage;

    [Fact]
    public async Task should_not_grant_from_an_unsigned_wildcard_grant()
    {
        SeedForged(StoreEntitlementsHistory.EventSourceId, new EventStoreAccessGranted(_attacker, Everything, EventStoreAccessGrantSource.Command));
        (await Snapshot()).Allows(_attacker, "shop", null, StoreAccess.Read).ShouldBeFalse();
    }

    [Fact]
    public async Task should_not_grant_from_a_signed_grant_widened_to_every_store()
    {
        var signed = (EventStoreAccessGranted)StoreEntitlementsHistory.WithProof(
            new EventStoreAccessGranted(_attacker, Everything, EventStoreAccessGrantSource.Command, EventStore: "shop", Namespace: "tenant"),
            EventSequenceNumber.BeforeFirst,
            _proof);
        SeedForged(StoreEntitlementsHistory.EventSourceId, signed with { EventStore = null, Namespace = null });
        (await Snapshot()).Allows(_attacker, "other", null, StoreAccess.Read).ShouldBeFalse();
    }

    [Fact]
    public async Task should_fail_closed_on_an_unsigned_wildcard_revocation()
    {
        await _entitlements.Grant(new(_attacker, null, null, StoreAccess.Read, EventStoreAccessGrantSource.Command));
        SeedForged(StoreEntitlementsHistory.EventSourceId, new EventStoreAccessRevoked(_attacker));
        (await CreateEntitlements().GetSnapshot()).Grants.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_only_read_changes_from_the_system_event_store()
    {
        await Snapshot();
        _storage.DidNotReceive().GetEventStore(Arg.Is<EventStoreName>(store => store != EventStoreName.System));
    }

    async Task<StoreEntitlementsSnapshot> Snapshot() => new(await CreateEntitlements().GetSnapshot());
}
