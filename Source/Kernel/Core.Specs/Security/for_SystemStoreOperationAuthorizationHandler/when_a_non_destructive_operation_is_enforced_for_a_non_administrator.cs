// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_non_destructive_operation_is_enforced_for_a_non_administrator : given.an_operation_policy
{
    void Establish()
    {
        _enforceDestructive = true;
        _operation = new RetryPartition("System", "default", Secret, string.Empty, "partition");
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_keep_the_existing_audit_behavior() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_log_that_the_policy_would_deny() => _logger.Entries.Single().Message.ShouldContain("would deny");
}
