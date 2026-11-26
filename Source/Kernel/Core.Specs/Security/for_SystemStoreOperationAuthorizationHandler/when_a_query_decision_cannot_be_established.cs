// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_query_decision_cannot_be_established : given.a_query_operation_policy
{
    void Establish()
    {
        Query(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "System" });
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>())
            .Returns(call => AuthorizationDecision.Deny(AuthorizationReason.Failed, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_fail_closed_in_full_audit_mode() => _context.HasSucceeded.ShouldBeFalse();
    [Fact] void should_not_log_a_would_deny_entry() => _logger.Entries.ShouldBeEmpty();
}
