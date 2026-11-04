// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_operating_on_an_application_store : given.a_decision_point
{
    async Task Because() => _decision = await Decide(_principal, WellKnownCapabilities.OperateEventStore, AuthorizationScope.ForEventStore("shop"));

    [Fact] void should_allow_as_not_enrolled() => ShouldHaveDecided(AuthorizationOutcome.Allowed, AuthorizationReason.NotEnrolled);
    [Fact] void should_not_call_the_lookup() => LookupShouldNotHaveBeenCalled();
}
