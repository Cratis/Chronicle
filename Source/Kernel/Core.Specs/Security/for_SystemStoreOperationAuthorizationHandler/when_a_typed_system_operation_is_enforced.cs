// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_typed_system_operation_is_enforced : given.an_operation_policy
{
    void Establish()
    {
        _enforce = true;
        _operation = new { EventStore = EventStoreName.System };
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_require_administrator_privilege() => _context.HasSucceeded.ShouldBeFalse();
}
