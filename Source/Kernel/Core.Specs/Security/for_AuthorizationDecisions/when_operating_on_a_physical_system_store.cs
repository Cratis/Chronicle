// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_operating_on_a_physical_system_store : given.a_decision_point
{
    AuthorizationDecision _allowed;

    async Task Because()
    {
        var scope = AuthorizationScope.ForEventStore("SyStEm+namespace");
        _decision = await Decide(_principal, WellKnownCapabilities.OperateEventStore, scope);
        _administrators.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(true);
        _allowed = await Decide(_principal, WellKnownCapabilities.OperateEventStore, scope);
    }

    [Fact] void should_require_administrator_privilege() => ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    [Fact] void should_allow_an_administrator() => ShouldHaveDecided(_allowed, AuthorizationOutcome.Allowed, AuthorizationReason.Granted);
    [Fact] void should_never_return_the_ordinary_store_exemption() => _decision.Reason.ShouldNotEqual(AuthorizationReason.NotEnrolled);
}
