// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EventStoreEntitlements;

public class when_reconciling_configuration : given.an_entitlements_grain
{
    StoreEntitlementsState _state;
    int _afterFirst;
    int _afterSecond;

    async Task Because()
    {
        await _entitlements.Grant(_grant);
        var configured = new ConfiguredGrant(new(PrincipalKey.LocalIssuer, "configured"), "shop", null, StoreAccess.Read);
        await _entitlements.Reconcile([configured]);
        _afterFirst = _appendCalls;
        await _entitlements.Reconcile([configured]);
        _afterSecond = _appendCalls;
        _entitlements = CreateEntitlements();
        await _entitlements.Reconcile([configured with { EventStore = "other" }]);
        await _entitlements.Reconcile([]);
        _state = await _entitlements.GetSnapshot();
    }

    [Fact] void should_not_reappend_unchanged_configuration() => _afterSecond.ShouldEqual(_afterFirst);
    [Fact] void should_preserve_command_grants() => _state.Grants.ShouldContainOnly(_grant);
    [Fact] void should_revoke_removed_configuration() => Recorded<EventStoreAccessRevoked>().Count().ShouldEqual(2);
}
