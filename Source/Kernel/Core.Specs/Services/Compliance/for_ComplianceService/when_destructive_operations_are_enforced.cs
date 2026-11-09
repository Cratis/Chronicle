// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_destructive_operations_are_enforced : given.a_compliance_boundary
{
    void Establish() => Configure(enforce: false, enforceDestructive: true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_the_other_stores_for_a_non_administrator_and_keep_the_own_store(bool allowNew)
    {
        Deny(OtherEventStore);
        Deny(ThirdEventStore);
        (await Operate(allowNew)).ShouldEqual([OtherEventStore.Value, ThirdEventStore.Value]);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
        await ShouldHaveTouchedOwnStore(allowNew);
        string.Join(' ', _audit.Values).ShouldContain("enforce-other-stores");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_partial_success_without_mutating_denied_stores(bool allowNew)
    {
        Deny(OtherEventStore);
        var exception = await Catch.Exception(() => allowNew
            ? _service.AllowNewEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context)
            : _service.DeleteEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context));
        ((RpcException)exception).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        ShouldNotHaveTouched(OtherEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_cover_every_store_for_an_administrator(bool allowNew)
    {
        (await Operate(allowNew)).ShouldBeEmpty();
        foreach (var store in new[] { EventStore, OtherEventStore, ThirdEventStore })
        {
            if (allowNew) await _keyStore.Received(1).AuthorizeConfirmedSuccessorFor(store, EventStoreNamespace, Identifier);
            else await _keyStore.Received(1).DeleteThroughFor(store, EventStoreNamespace, Identifier, Arg.Any<string>());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_allow_the_own_store_when_it_is_the_only_store(bool allowNew)
    {
        _storage.GetEventStores().Returns([EventStore]);
        Deny(OtherEventStore);
        (await Operate(allowNew)).ShouldBeEmpty();
        await ShouldHaveTouchedOwnStore(allowNew);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_keep_the_requested_store_decision_with_the_compliance_flag_off(bool allowNew)
    {
        Deny(EventStore);
        Deny(OtherEventStore);
        (await Operate(allowNew)).ShouldEqual([OtherEventStore.Value]);
        await ShouldHaveTouchedOwnStore(allowNew);
    }

    Task ShouldHaveTouchedOwnStore(bool allowNew) => allowNew
        ? _keyStore.Received(1).AuthorizeConfirmedSuccessorFor(EventStore, EventStoreNamespace, Identifier)
        : _keyStore.Received(1).DeleteThroughFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<string>());
}
