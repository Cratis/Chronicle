// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_operating_without_a_resolved_store : given.a_decision_point
{
    void Establish() => _administrators.IsAuthorized(_principal).Returns(false);

    async Task Because() => _decision = await Decide(_principal, WellKnownCapabilities.OperateEventStore, AuthorizationScope.Kernel);

    [Fact] void should_require_an_administrator() => ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    [Fact] void should_call_the_lookup() => _administrators.Received(1).IsAuthorized(_principal);
}
