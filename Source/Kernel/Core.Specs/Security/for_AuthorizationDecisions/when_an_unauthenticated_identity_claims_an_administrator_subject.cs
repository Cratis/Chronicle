// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_an_unauthenticated_identity_claims_an_administrator_subject : given.a_decision_point
{
    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)]));
        _administrators.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(true);
    }

    async Task Because() => _decision = await Decide(_principal, WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel);

    [Fact] void should_deny_as_no_principal() => ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.NoPrincipal);
    [Fact] void should_not_call_the_lookup() => LookupShouldNotHaveBeenCalled();
}
