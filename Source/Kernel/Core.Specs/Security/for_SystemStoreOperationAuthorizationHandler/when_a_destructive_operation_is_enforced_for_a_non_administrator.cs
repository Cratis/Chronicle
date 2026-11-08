// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_destructive_operation_is_enforced_for_a_non_administrator : given.an_operation_policy
{
    void Establish() => _enforceDestructive = true;

    Task Because() => AuthorizeOperation();

    [Fact] void should_refuse_the_operation() => _context.HasSucceeded.ShouldBeFalse();
    [Fact] void should_log_the_decision() => _logger.Entries.Single().Message.ShouldContain("ReplayObserver is refused");
    [Fact] void should_not_log_tokens_or_secrets() => _logger.Entries.Single().Message.ShouldNotContain(Secret);
}
