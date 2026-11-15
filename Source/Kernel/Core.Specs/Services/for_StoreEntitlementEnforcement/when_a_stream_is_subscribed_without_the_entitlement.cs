// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Services.for_StoreEntitlementEnforcement;

public class when_a_stream_is_subscribed_without_the_entitlement : given.a_store_entitlement_caller
{
    RpcException _refusal;
    int _audited;

    async Task Because()
    {
        _refusal = await Catch.Exception(() => SystemStoreGuard.Ensure(ContextFor(true), new("shop", "default"), StoreAccess.Read, () => Observable.Return(1)).ToTask()) as RpcException;
        _audited = await SystemStoreGuard.Ensure(ContextFor(false), new("shop", "default"), StoreAccess.Read, () => Observable.Return(1)).FirstAsync().ToTask();
    }

    [Fact] void should_refuse_the_subscription_when_enforcing() => _refusal.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_deliver_the_stream_when_auditing() => _audited.ShouldEqual(1);
}
