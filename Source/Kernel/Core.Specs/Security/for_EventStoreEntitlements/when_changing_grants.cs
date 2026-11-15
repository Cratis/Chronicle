// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EventStoreEntitlements;

public class when_changing_grants : given.an_entitlements_grain
{
    [Theory]
    [InlineData("System")]
    [InlineData("system")]
    public async Task should_reject_a_system_grant(string store) => await Assert.ThrowsAsync<ArgumentException>(() => _entitlements.Grant(_grant with { EventStore = store }));

    [Fact]
    async Task should_replace_access_instead_of_accumulating_it()
    {
        await _entitlements.Grant(_grant);
        await _entitlements.Grant(_grant with { Access = StoreAccess.Append });
        (await CreateEntitlements().GetSnapshot()).Grants.Single().Access.ShouldEqual(StoreAccess.Append);
    }

    [Fact]
    async Task should_return_false_for_an_unknown_revocation() => (await _entitlements.Revoke(_grant)).ShouldBeFalse();

    [Fact]
    async Task should_preserve_a_command_grant_at_a_configured_identity()
    {
        await _entitlements.Grant(_grant);
        await _entitlements.Reconcile([new(_grant.Holder, _grant.EventStore, _grant.Namespace, StoreAccess.Manage)]);
        (await _entitlements.GetSnapshot()).Grants.ShouldContainOnly(_grant);
    }

    [Fact]
    async Task should_update_changed_configured_access()
    {
        await _entitlements.Reconcile([new(_grant.Holder, _grant.EventStore, _grant.Namespace, StoreAccess.Read)]);
        await _entitlements.Reconcile([new(_grant.Holder, _grant.EventStore, _grant.Namespace, StoreAccess.Append)]);
        (await _entitlements.GetSnapshot()).Grants.Single().Access.ShouldEqual(StoreAccess.Append);
    }
}
