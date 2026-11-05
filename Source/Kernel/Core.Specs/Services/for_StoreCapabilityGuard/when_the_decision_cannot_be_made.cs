// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_the_decision_cannot_be_made : given.a_store_capability_caller
{
    bool _permitted;

    void Establish() => Decide(AuthorizationReason.Failed, false);

    async Task Because() => _permitted = await StoreCapabilityGuard.Permits(_context, new("shop", Capability: WellKnownCapabilities.ReadAlerts.Value));

    [Fact] void should_refuse_the_query() => _permitted.ShouldBeFalse();
}
