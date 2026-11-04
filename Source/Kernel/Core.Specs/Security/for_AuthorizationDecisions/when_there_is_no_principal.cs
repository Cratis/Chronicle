// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_there_is_no_principal : given.a_decision_point
{
    async Task Because() => _decision = await Decide(null, WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel);

    [Fact] void should_deny_as_no_principal() => ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.NoPrincipal);
    [Fact] void should_not_call_the_lookup() => LookupShouldNotHaveBeenCalled();
}
