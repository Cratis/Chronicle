// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_an_administrator_requests_the_enrollment_capability_with_no_application_configured : given.a_decision_point
{
    void Establish() => _administrators.IsAuthorized(_principal).Returns(true);

    [Fact]
    public async Task should_allow_it()
    {
        var decision = await Decide(_principal, WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Allowed, AuthorizationReason.Granted);
    }
}
