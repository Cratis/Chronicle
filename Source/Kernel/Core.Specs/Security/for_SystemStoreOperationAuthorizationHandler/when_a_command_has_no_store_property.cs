// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_command_has_no_store_property : given.an_operation_policy
{
    void Establish()
    {
        _enforce = true;
        _operation = new RemoveApplication(Guid.NewGuid());
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_treat_it_as_a_system_operation() => _context.HasSucceeded.ShouldBeFalse();
}
