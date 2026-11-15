// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_the_capability_differs_only_in_case : given.a_decision_point
{
    void Establish() => _administrators.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(true);

    async Task Because() => _decision = await Decide(_principal, WellKnownCapabilities.AdministerSecurity.Value.ToUpperInvariant(), AuthorizationScope.Kernel);

    [Fact] void should_deny_as_unknown() => ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.UnknownCapability);
    [Fact] void should_not_call_the_lookup() => LookupShouldNotHaveBeenCalled();
}
