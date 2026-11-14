// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_an_application_is_not_configured_for_enrollment : given.a_decision_point
{
    void Establish()
    {
        _enrollingApplications = ["billing-app"];
        _administrators.IsAuthorized(default!).ReturnsForAnyArgs(false);
    }

    [Fact]
    public async Task should_deny_an_application_not_on_the_list()
    {
        var decision = await Decide(ApplicationToken("other-app"), WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }

    [Fact]
    public async Task should_not_match_a_different_casing()
    {
        var decision = await Decide(ApplicationToken("Billing-App"), WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }

    [Fact]
    public async Task should_not_grant_a_user_that_is_named_like_the_application()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString()), new Claim("name", "billing-app"), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)],
            "Bearer"));
        var decision = await Decide(user, WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }

    [Fact]
    public async Task should_not_grant_an_unauthenticated_caller_with_the_claim()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "billing-app")]));
        var decision = await Decide(anonymous, WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NoPrincipal);
    }
}
