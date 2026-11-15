// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_StoreEntitlementEnforcement;

public class when_an_entitled_call_is_made : given.a_store_entitlement_caller
{
    Exception? _error;

    void Establish() => _granted = true;

    async Task Because() => _error = await Catch.Exception(() => SystemStoreGuard.Ensure(ContextFor(true), new("shop", "default"), StoreAccess.Read | StoreAccess.Append));

    [Fact] void should_let_the_call_through() => _error.ShouldBeNull();
    [Fact] void should_ask_for_both_accesses() => _asked.Count.ShouldEqual(2);
    [Fact] void should_not_call_the_administrator_privileges() => _privileges.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_call_any_grain() => _grains.ReceivedCalls().ShouldBeEmpty();
}
