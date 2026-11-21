// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security.for_FederationManagementAuthorizationHandler;

public class when_the_decision_denies : Specification
{
    FederationManagementAuthorizationHandler _handler;
    AuthorizationHandlerContext _context;

    void Establish()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity("spec"));
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        _context = new AuthorizationHandlerContext([new FederationManagementRequirement()], principal, null);
        _handler = new FederationManagementAuthorizationHandler(decisions);
    }

    Task Because() => _handler.HandleAsync(_context);

    [Fact] void should_not_succeed() => _context.HasSucceeded.ShouldBeFalse();
}
