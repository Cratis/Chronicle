// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_a_configured_application_requests_the_management_capability : given.a_decision_point
{
    void Establish() => _managingApplications = ["operator-app", "other-operator"];

    [Fact]
    public async Task should_allow_it_without_consulting_the_administrators()
    {
        var decision = await Decide(ApplicationToken("operator-app"), WellKnownCapabilities.ManageFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Allowed, AuthorizationReason.Granted);
        LookupShouldNotHaveBeenCalled();
    }

    [Fact]
    public async Task should_allow_another_configured_application()
    {
        var decision = await Decide(ApplicationToken("other-operator"), WellKnownCapabilities.ManageFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Allowed, AuthorizationReason.Granted);
    }
}
