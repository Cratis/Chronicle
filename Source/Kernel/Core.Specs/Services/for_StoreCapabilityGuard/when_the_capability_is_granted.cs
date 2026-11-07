// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_the_capability_is_granted : given.a_store_capability_caller
{
    bool _permitted;

    void Establish() => Decide(AuthorizationReason.Granted, true);

    async Task Because() => _permitted = await StoreCapabilityGuard.Permits(_context, new("shop", "tenant", Capability: WellKnownCapabilities.ReadAlerts.Value));

    [Fact] void should_permit_the_query() => _permitted.ShouldBeTrue();
    [Fact] void should_decide_at_the_namespace_of_the_requested_store() => _asked.Single().Scope.ShouldEqual(AuthorizationScope.ForNamespace("shop", "tenant"));
    [Fact] void should_not_record_a_decision() => _loggers.Messages.ShouldBeEmpty();
}
