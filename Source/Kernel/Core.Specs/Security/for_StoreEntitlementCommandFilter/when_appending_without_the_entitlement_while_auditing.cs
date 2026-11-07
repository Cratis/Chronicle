// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_appending_without_the_entitlement_while_auditing : given.a_command_filter
{
    async Task Because() => _result = await _filter.OnExecution(ContextFor(new AppendLike("shop", "tenant")));

    [Fact] void should_not_refuse() => _result.ShouldBeNull();

    [Fact] async Task should_log_the_decision() => (await AuditLogged(WellKnownCapabilities.AppendEventStore.Value)).ShouldBeTrue();
}
