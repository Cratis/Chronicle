// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security.for_FederationEnrollmentAuthorizationHandler;

public class when_the_decision_denies : Specification
{
    AuthorizationHandlerContext _context;

    void Establish()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity("spec"));
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        _context = new AuthorizationHandlerContext([new FederationEnrollmentRequirement()], principal, null);
        _handler = new FederationEnrollmentAuthorizationHandler(decisions);
    }

    FederationEnrollmentAuthorizationHandler _handler;

    Task Because() => _handler.HandleAsync(_context);

    [Fact] void should_not_succeed() => _context.HasSucceeded.ShouldBeFalse();
}
