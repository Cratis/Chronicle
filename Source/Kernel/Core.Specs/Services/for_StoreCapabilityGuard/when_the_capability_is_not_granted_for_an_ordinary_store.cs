// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_the_capability_is_not_granted_for_an_ordinary_store : given.a_store_capability_caller
{
    bool _permitted;

    void Establish() => Decide(AuthorizationReason.NotGranted, false);

    async Task Because() => _permitted = await StoreCapabilityGuard.Permits(_context, new("shop", Capability: WellKnownCapabilities.ReadAlerts.Value));

    [Fact] void should_keep_the_query_open_for_the_ordinary_store() => _permitted.ShouldBeTrue();
    [Fact] void should_decide_at_the_store_when_no_namespace_is_addressed() => _asked.Single().Scope.ShouldEqual(AuthorizationScope.ForEventStore("shop"));
    [Fact] void should_record_the_decision() => _loggers.Messages.Single().ShouldContain(WellKnownCapabilities.ReadAlerts.Value);
    [Fact] void should_record_it_as_a_tolerated_decision_at_debug_level() => _loggers.Levels.Single().ShouldEqual(LogLevel.Debug);
    [Fact] void should_state_that_the_query_is_allowed() => _loggers.Messages.Single().ShouldContain("allowed");
}
