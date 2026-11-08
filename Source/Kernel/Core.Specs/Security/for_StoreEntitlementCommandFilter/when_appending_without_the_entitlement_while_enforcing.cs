// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_appending_without_the_entitlement_while_enforcing : given.a_command_filter
{
    void Establish() => _enforce = true;

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new AppendLike("shop", "tenant")));

    [Fact] void should_refuse() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_use_the_common_message() => _result.AuthorizationFailureReason.ShouldEqual(StoreEntitlementCommandFilter.Refused);

    [Fact] void should_decide_append_at_the_addressed_store() => Decided().Single().ShouldEqual(("shop", WellKnownCapabilities.AppendEventStore.Value));
}
