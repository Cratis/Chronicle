// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_an_unlisted_application_requests_the_management_capability : given.a_decision_point
{
    void Establish()
    {
        _managingApplications = ["operator-app"];
        _enrollingApplications = ["billing-app"];
        _administrators.IsAuthorized(default!).ReturnsForAnyArgs(false);
    }

    [Fact]
    public async Task should_deny_an_application_that_is_only_allowed_to_enroll()
    {
        var decision = await Decide(ApplicationToken("billing-app"), WellKnownCapabilities.ManageFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }

    [Fact]
    public async Task should_deny_an_application_on_no_list()
    {
        var decision = await Decide(ApplicationToken("someone-else"), WellKnownCapabilities.ManageFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }
}
