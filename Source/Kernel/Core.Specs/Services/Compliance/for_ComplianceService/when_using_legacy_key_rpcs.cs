// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_using_legacy_key_rpcs : given.a_compliance_boundary
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_refuse_partial_coverage_before_any_mutation(bool allowNew, bool lookupFails)
    {
        Deny(OtherEventStore, throws: lookupFails);
        var exception = await Catch.Exception(() => OperateLegacy(allowNew));
        ((RpcException)exception).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        ShouldNotHaveTouched(EventStore);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_report_a_listing_failure_before_any_mutation(bool allowNew)
    {
        _storage.GetEventStores().Returns(Task.FromException<IEnumerable<Concepts.EventStoreName>>(new InvalidOperationException("listing unavailable")));
        var exception = await Catch.Exception(() => OperateLegacy(allowNew));
        exception.ShouldBeOfExactType<RpcException>();
        ((RpcException)exception).StatusCode.ShouldEqual(StatusCode.Unavailable);
        ShouldNotHaveTouched(EventStore);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
        _audit.Entries.ShouldEqual(1);
        string.Join(' ', _audit.Values).ShouldNotContain("listing unavailable");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_cover_the_requested_store_even_when_the_listing_omits_it(bool allowNew)
    {
        _storage.GetEventStores().Returns([OtherEventStore]);
        await OperateLegacy(allowNew);
        if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
        else await _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_mutate_all_targets_when_fully_covered(bool allowNew)
    {
        await OperateLegacy(allowNew);
        foreach (var store in new[] { EventStore, OtherEventStore, ThirdEventStore })
        {
            if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(store, EventStoreNamespace, Identifier);
            else await _keyStore.Received(1).DeleteFor(store, EventStoreNamespace, Identifier);
        }
        ShouldNotHaveTouched(Concepts.EventStoreName.System);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_not_reveal_other_stores_when_the_requested_store_is_denied(bool allowNew)
    {
        Deny(EventStore);
        var exception = await Catch.Exception(() => allowNew
            ? _service.AllowNewEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context)
            : _service.DeleteEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context));
        var rpc = (RpcException)exception;
        rpc.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        rpc.Trailers.Count.ShouldEqual(0);
        rpc.Status.Detail.ShouldNotContain(OtherEventStore.Value);
        ShouldNotHaveTouched(EventStore);
        ShouldNotHaveTouched(OtherEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_name_uncovered_stores_in_the_legacy_failure(bool allowNew)
    {
        Deny(OtherEventStore);
        var exception = await Catch.Exception(() => allowNew
            ? _service.AllowNewEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context)
            : _service.DeleteEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context));
        var rpc = (RpcException)exception;
        rpc.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        rpc.Status.Detail.ShouldContain(OtherEventStore.Value);
        rpc.Status.Detail.ShouldNotContain(Identifier.Value);
    }

    Task OperateLegacy(bool allowNew) => allowNew
        ? _service.AllowNewEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context)
        : _service.DeleteEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context);
}
