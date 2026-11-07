// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_StoreEntitlementEnforcement;

public class when_a_generated_query_reads_a_store_without_the_entitlement : given.a_store_entitlement_caller
{
    bool _enforcedResult;
    bool _auditedResult;

    async Task Because()
    {
        _enforcedResult = (await QueryExecutor.Execute(ContextFor(true), new("shop"), () => Task.FromResult(1))).IsAuthorized;
        _auditedResult = (await QueryExecutor.Execute(ContextFor(false), new("shop"), () => Task.FromResult(1))).IsAuthorized;
    }

    [Fact] void should_refuse_the_query_when_enforcing() => _enforcedResult.ShouldBeFalse();
    [Fact] void should_let_the_query_run_when_auditing() => _auditedResult.ShouldBeTrue();
    [Fact] void should_ask_for_read_access() => (_asked.Count > 0 && _asked.TrueForAll(asked => asked.Capability == WellKnownCapabilities.ReadEventStore && asked.Scope == AuthorizationScope.ForEventStore("shop"))).ShouldBeTrue();
}
