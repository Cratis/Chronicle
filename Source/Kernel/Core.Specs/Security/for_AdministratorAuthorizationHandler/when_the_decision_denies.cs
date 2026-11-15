// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationHandler;

public class when_the_decision_denies : Specification
{
    IAuthorizationDecisions _decisions;
    AuthorizationHandlerContext _context;

    void Establish()
    {
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        _context = new AuthorizationHandlerContext([new AdministratorRequirement()], new ClaimsPrincipal(new ClaimsIdentity("spec")), null);
    }

    Task Because() => new AdministratorAuthorizationHandler(_decisions).HandleAsync(_context);

    [Fact] void should_not_succeed() => _context.HasSucceeded.ShouldBeFalse();
}
