// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security.for_FederationEnrollmentAuthorizationHandler;

public class when_the_decision_allows : Specification
{
    IAuthorizationDecisions _decisions;
    ClaimsPrincipal _principal;
    AuthorizationHandlerContext _context;

    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity("spec"));
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        _context = new AuthorizationHandlerContext([new FederationEnrollmentRequirement()], _principal, null);
    }

    Task Because() => new FederationEnrollmentAuthorizationHandler(_decisions).HandleAsync(_context);

    [Fact] void should_succeed() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_ask_for_the_enroll_capability_at_kernel_scope() =>
        _decisions.Received(1).Decide(_principal, WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
}
