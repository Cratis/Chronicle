// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_no_application_is_configured_for_enrollment : given.a_decision_point
{
    void Establish() => _administrators.IsAuthorized(default!).ReturnsForAnyArgs(false);

    [Fact]
    public async Task should_deny_an_application()
    {
        var decision = await Decide(ApplicationToken("billing-app"), WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }
}
