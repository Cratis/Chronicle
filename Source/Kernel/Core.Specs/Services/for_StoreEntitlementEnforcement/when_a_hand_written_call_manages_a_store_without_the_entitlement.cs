// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Services.for_StoreEntitlementEnforcement;

public class when_a_hand_written_call_manages_a_store_without_the_entitlement : given.a_store_entitlement_caller
{
    RpcException _refusal;
    Exception? _audited;

    async Task Because()
    {
        _refusal = await Catch.Exception(() => SystemStoreGuard.Ensure(ContextFor(true), new("shop", "default"), StoreAccess.Manage)) as RpcException;
        _audited = await Catch.Exception(() => SystemStoreGuard.Ensure(ContextFor(false), new("shop", "default"), StoreAccess.Manage));
    }

    [Fact] void should_refuse_with_permission_denied_when_enforcing() => _refusal.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_let_the_call_through_when_auditing() => _audited.ShouldBeNull();
    [Fact] void should_ask_for_manage_at_the_namespace() => _asked[0].ShouldEqual((WellKnownCapabilities.ManageEventStore, AuthorizationScope.ForNamespace("shop", "default")));
}
