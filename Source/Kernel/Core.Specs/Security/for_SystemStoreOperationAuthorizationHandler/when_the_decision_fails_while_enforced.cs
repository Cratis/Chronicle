// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_the_decision_fails_while_enforced : given.an_operation_policy
{
    void Establish()
    {
        _enforce = true;
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Deny(AuthorizationReason.Failed, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
        _decisions = decisions;
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_not_succeed() => _context.HasSucceeded.ShouldBeFalse();
}
