// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EventStoreAccessPreflight;

public class when_listing_uncovered_observations : Specification
{
    readonly PrincipalKey _holder = new("issuer", "subject");
    IStoreEntitlements _entitlements;
    IGrainFactory _grains;
    IEnumerable<EventStoreAccessPreflight> _result;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        _entitlements = Substitute.For<IStoreEntitlements>();
        _grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(_entitlements);
        _entitlements.GetSnapshot().Returns(new StoreEntitlementsState(1, [new(_holder, "orders", null, StoreAccess.Read, EventStoreAccessGrantSource.Command)]));
        var observations = Substitute.For<IStoreEntitlementObservations>();
        _grains.GetGrain<IStoreEntitlementObservations>(IStoreEntitlementObservations.Key).Returns(observations);
        var now = DateTimeOffset.UtcNow;
        observations.GetSnapshot().Returns([
            new(_holder, "orders", "tenant", StoreAccess.Read, now, now, 1),
            new(_holder, "orders", "tenant", StoreAccess.Append, now, now, 2),
            new(_holder, "other", null, StoreAccess.Read, now, now, 1)
        ]);
    }

    async Task Because() => _result = await EventStoreAccessPreflight.Uncovered(_grains);

    [Fact] void should_omit_currently_covered_entries() => _result.Count().ShouldEqual(2);
    [Fact] void should_retain_the_observed_count() => _result.Single(entry => entry.Access == StoreAccess.Append).Count.ShouldEqual(2L);
    [Fact] void should_never_create_a_grant() => _entitlements.DidNotReceive().Grant(Arg.Any<EventStoreAccessGrant>());
    [Fact] void should_never_reconcile() => _entitlements.DidNotReceive().Reconcile(Arg.Any<IEnumerable<ConfiguredGrant>>());
}
