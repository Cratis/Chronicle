// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_using_legacy_key_rpcs : given.a_compliance_boundary
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_complete_authorized_work_for_partial_coverage(bool allowNew)
    {
        Deny(OtherEventStore);
        var exception = await Catch.Exception(() => allowNew
            ? _service.AllowNewEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context)
            : _service.DeleteEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context));
        exception.ShouldBeNull();
        ShouldNotHaveTouched(OtherEventStore);
        if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
        else await _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_apply_the_requested_store_operation_before_reporting_a_listing_failure(bool allowNew)
    {
        _storage.GetEventStores().Returns(Task.FromException<IEnumerable<Concepts.EventStoreName>>(new InvalidOperationException("listing unavailable")));
        var exception = await Catch.Exception(() => Operate(allowNew));
        exception.ShouldBeOfExactType<RpcException>();
        ((RpcException)exception).StatusCode.ShouldEqual(StatusCode.Unavailable);
        if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
        else await _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
        ShouldNotHaveTouched(OtherEventStore);
        _audit.Entries.ShouldEqual(1);
        string.Join(' ', _audit.Values).ShouldNotContain("listing unavailable");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_cover_the_requested_store_even_when_the_listing_omits_it(bool allowNew)
    {
        _storage.GetEventStores().Returns([OtherEventStore]);
        (await Operate(allowNew)).ShouldBeEmpty();
        if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
        else await _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
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
    public async Task should_not_return_denied_store_metadata_from_legacy_calls(bool allowNew)
    {
        Deny(OtherEventStore);
        var exception = await Catch.Exception(() => allowNew
            ? _service.AllowNewEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context)
            : _service.DeleteEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context));
        exception.ShouldBeNull();
    }
}
