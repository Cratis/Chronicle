// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_privilege_is_revoked_between_calls : given.a_decision_point
{
    AuthorizationDecision _first;

    void Establish() => _administrators.IsAuthorized(_principal).Returns(true, false);

    async Task Because()
    {
        var decisions = CreateDecisions();
        _first = await decisions.Decide(_principal, WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel);
        _decision = await decisions.Decide(_principal, WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel);
    }

    [Fact] void should_allow_the_first_call() => _first.Outcome.ShouldEqual(AuthorizationOutcome.Allowed);
    [Fact] void should_deny_the_second_call() => ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
}
