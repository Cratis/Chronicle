// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_the_capability_is_unknown : given.a_decision_point
{
    AuthorizationDecision _whileDisabled;

    void Establish() => _administrators.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(true);

    async Task Because()
    {
        _decision = await Decide(_principal, "chronicle.unknown", AuthorizationScope.Kernel);
        _authenticationEnabled = false;
        _whileDisabled = await Decide(_principal, "chronicle.unknown", AuthorizationScope.Kernel);
    }

    [Fact] void should_deny_as_unknown() => ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.UnknownCapability);
    [Fact] void should_deny_as_unknown_when_authentication_is_disabled()
    {
        _whileDisabled.Outcome.ShouldEqual(AuthorizationOutcome.Denied);
        _whileDisabled.Reason.ShouldEqual(AuthorizationReason.UnknownCapability);
    }

    [Fact] void should_not_call_the_lookup() => LookupShouldNotHaveBeenCalled();
}
