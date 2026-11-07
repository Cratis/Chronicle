// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_destructive_operation_is_not_enforced : given.an_operation_policy
{
    Task Because() => AuthorizeOperation();

    [Fact] void should_keep_the_existing_audit_behavior() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_log_that_the_policy_would_deny() => _logger.Entries.Single().Message.ShouldContain("would deny");
}
