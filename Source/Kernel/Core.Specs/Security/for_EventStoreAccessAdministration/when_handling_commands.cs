// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EventStoreAccessAdministration;

public class when_handling_commands : Specification
{
    [Fact]
    public async Task should_persist_only_the_explicit_command_grant()
    {
        var grains = Substitute.For<IGrainFactory>();
        var entitlements = Substitute.For<IStoreEntitlements>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(entitlements);
        await new GrantEventStoreAccess(new("issuer", "subject", true), "orders", false, "tenant", StoreAccess.Read).Handle(grains);
        await entitlements.Received(1).Grant(new(new("issuer", "subject", true), "orders", "tenant", StoreAccess.Read, EventStoreAccessGrantSource.Command));
        await entitlements.DidNotReceive().Reconcile(Arg.Any<IEnumerable<ConfiguredGrant>>());
    }

    [Fact]
    public async Task should_revoke_only_the_addressed_scope()
    {
        var grains = Substitute.For<IGrainFactory>();
        var entitlements = Substitute.For<IStoreEntitlements>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(entitlements);
        await new RevokeEventStoreAccess(new("issuer", "subject", false), null, true, null).Handle(grains);
        await entitlements.Received(1).Revoke(new(new("issuer", "subject"), null, null, StoreAccess.None, EventStoreAccessGrantSource.Command));
        await entitlements.DidNotReceive().Grant(Arg.Any<EventStoreAccessGrant>());
    }

    [Theory]
    [InlineData("System", false, StoreAccess.Read, false)]
    [InlineData("sYsTeM", false, StoreAccess.Manage, false)]
    [InlineData("orders", false, StoreAccess.None, false)]
    [InlineData("orders", false, (StoreAccess)8, false)]
    [InlineData("orders", false, StoreAccess.Read, true)]
    [InlineData(null, true, StoreAccess.Manage, true)]
    [InlineData(null, false, StoreAccess.Manage, false)]
    [InlineData("orders", true, StoreAccess.Read, false)]
    void should_validate_explicit_scope_and_access(string? store, bool all, StoreAccess access, bool valid)
    {
        var command = new GrantEventStoreAccess(new("issuer", "subject", false), store is null ? null : new Concepts.EventStoreName(store), all, null, access);
        new GrantEventStoreAccessValidator().Validate(command).IsValid.ShouldEqual(valid);
    }
}
