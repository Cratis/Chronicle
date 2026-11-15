// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_authentication_is_disabled : given.a_decision_point
{
    List<AuthorizationDecision> _decisions;
    AuthorizationDecision _withoutPrincipal;

    void Establish() => _authenticationEnabled = false;

    async Task Because()
    {
        _decisions = [];
        foreach (var capability in new[] { WellKnownCapabilities.AdministerSecurity, WellKnownCapabilities.OperateEventStore, WellKnownCapabilities.AccessSystemStore })
        {
            _decisions.Add(await Decide(_principal, capability, AuthorizationScope.Kernel));
        }
        _withoutPrincipal = await Decide(null, WellKnownCapabilities.AccessSystemStore, AuthorizationScope.ForEventStore(EventStoreName.System));
    }

    [Fact] void should_allow_every_known_capability() => _decisions.TrueForAll(d => d.Outcome == AuthorizationOutcome.Allowed && d.Reason == AuthorizationReason.AuthenticationDisabled).ShouldBeTrue();
    [Fact] void should_allow_without_a_principal()
    {
        _withoutPrincipal.Outcome.ShouldEqual(AuthorizationOutcome.Allowed);
        _withoutPrincipal.Reason.ShouldEqual(AuthorizationReason.AuthenticationDisabled);
    }

    [Fact] void should_not_call_the_lookup() => LookupShouldNotHaveBeenCalled();
}
