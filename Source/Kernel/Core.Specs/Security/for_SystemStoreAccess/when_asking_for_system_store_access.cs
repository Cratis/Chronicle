// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.for_SystemStoreAccess;

public class when_asking_for_system_store_access : Specification
{
    IAuthorizationDecisions _decisions;
    SystemStoreAccessVerdict _verdict;
    ClaimsPrincipal _principal;
    bool _allowed;

    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity("spec"));
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        _verdict = new SystemStoreAccessVerdict(_decisions);
    }

    async Task Because() => _allowed = await _verdict.IsAllowed(_principal);

    [Fact] void should_ask_for_system_store_access() =>
        _decisions.Received(1).Decide(_principal, WellKnownCapabilities.AccessSystemStore, AuthorizationScope.ForEventStore(EventStoreName.System));
    [Fact] void should_follow_the_decision() => _allowed.ShouldBeTrue();
}
