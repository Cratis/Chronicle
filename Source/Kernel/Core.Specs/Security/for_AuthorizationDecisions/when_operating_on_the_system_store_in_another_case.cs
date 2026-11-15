// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_operating_on_the_system_store_in_another_case : given.a_decision_point
{
    void Establish() => _administrators.IsAuthorized(_principal).Returns(false);

    async Task Because() => _decision = await Decide(_principal, WellKnownCapabilities.OperateEventStore, AuthorizationScope.ForEventStore("SYSTEM"));

    [Fact] void should_decide_by_privilege() => ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    [Fact] void should_call_the_lookup() => _administrators.Received(1).IsAuthorized(_principal);
}
