// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Compliance;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_the_legacy_rpc_has_incomplete_coverage : given.a_compliance_boundary
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_refuse_partial_success_with_either_destructive_setting(bool allowNew, bool enforceDestructive)
    {
        Configure(enforce: true, enforceDestructive: enforceDestructive);
        Deny(OtherEventStore);
        var error = await Catch.Exception(() => OperateLegacy(allowNew));
        ((RpcException)error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        ((RpcException)error).Status.Detail.ShouldContain(OtherEventStore.Value);
        ShouldNotHaveTouched(EventStore);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_incomplete_listing_with_unconfigured_audit_defaults(bool allowNew)
    {
        Configure(enforce: false, useDefaults: true);
        _storage.GetEventStores().Returns(Task.FromException<IEnumerable<Concepts.EventStoreName>>(new InvalidOperationException("unavailable")));
        var error = await Catch.Exception(() => OperateLegacy(allowNew));
        ((RpcException)error).StatusCode.ShouldEqual(StatusCode.Unavailable);
        ShouldNotHaveTouched(EventStore);
        ShouldNotHaveTouched(OtherEventStore);
        ShouldNotHaveTouched(ThirdEventStore);
    }

    Task OperateLegacy(bool allowNew) => allowNew
        ? _service.AllowNewEncryptionKey(new AllowNewEncryptionKeyRequest { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context)
        : _service.DeleteEncryptionKey(new DeleteEncryptionKeyRequest { EventStore = EventStore, Namespace = EventStoreNamespace, Identifier = Identifier }, _context);
}
