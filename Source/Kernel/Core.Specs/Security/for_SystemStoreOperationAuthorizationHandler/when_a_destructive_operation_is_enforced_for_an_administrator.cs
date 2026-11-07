// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_destructive_operation_is_enforced_for_an_administrator : given.an_operation_policy
{
    void Establish()
    {
        _enforceDestructive = true;
        _authorization.IsAuthorized(_principal).Returns(true);
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_operation() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_not_log_a_decision() => _logger.Entries.ShouldBeEmpty();
}
