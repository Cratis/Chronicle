// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_system_operations_are_enforced : given.an_operation_policy
{
    void Establish() => _enforce = true;

    Task Because() => AuthorizeOperation();

    [Fact] void should_deny_the_non_administrator() => _context.HasSucceeded.ShouldBeFalse();
    [Fact] void should_not_describe_an_enforced_denial_as_audit_only() => _logger.Entries.ShouldBeEmpty();
}
