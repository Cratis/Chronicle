// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_an_external_authority_issues_the_application_token : given.a_decision_point
{
    void Establish()
    {
        _internalAuthority = false;
        _enrollingApplications = ["billing-app"];
        _administrators.IsAuthorized(default!).ReturnsForAnyArgs(false);
    }

    [Fact]
    public async Task should_match_the_client_id_claim()
    {
        var token = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "service-account"), new Claim("client_id", "billing-app")], "Bearer"));
        var decision = await Decide(token, WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Allowed, AuthorizationReason.Granted);
    }

    [Fact]
    public async Task should_not_trust_a_name_claim()
    {
        var decision = await Decide(ApplicationToken("billing-app"), WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }
}
