// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.when_defaults_apply;

public class when_a_non_administrator_operates_across_stores : given.a_compliance_boundary
{
    void Establish()
    {
        Configure(enforce: false, useDefaults: true);
        HoldIdentifierOnlyIn(EventStore, OtherEventStore);
        Deny(OtherEventStore);
        Deny(ThirdEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_apply_the_requested_store_and_report_only_the_affected_other_store(bool allowNew)
    {
        (await Operate(allowNew)).ShouldEqual([OtherEventStore.Value]);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
        if (allowNew) await _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
        else await _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_answer_the_legacy_calls_with_permission_denied_and_the_affected_store(bool allowNew)
    {
        var exception = await Catch.Exception(() => allowNew
            ? _service.AllowNewEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context)
            : _service.DeleteEncryptionKey(new() { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context));
        var rpc = (RpcException)exception;
        rpc.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        rpc.Trailers.Any(entry => entry.Key == "not-covered-event-stores-bin").ShouldBeTrue();
        ShouldNotHaveTouched(OtherEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_not_report_a_store_that_is_not_affected(bool allowNew)
    {
        HoldIdentifierOnlyIn(EventStore);
        (await Operate(allowNew)).ShouldBeEmpty();
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_count_a_store_whose_check_fails_as_affected(bool allowNew)
    {
        HoldIdentifierOnlyIn(EventStore);
        FailChecksFor(ThirdEventStore);
        (await Operate(allowNew)).ShouldEqual([ThirdEventStore.Value]);
        ShouldNotHaveTouched(ThirdEventStore);
        string.Join(' ', _audit.Values).ShouldNotContain("sensitive lookup detail");
    }
}
