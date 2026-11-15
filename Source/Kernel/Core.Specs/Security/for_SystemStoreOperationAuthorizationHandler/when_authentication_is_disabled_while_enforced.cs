// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_authentication_is_disabled_while_enforced : given.an_operation_policy
{
    void Establish()
    {
        _enforce = true;
        _authenticationEnabled = false;
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_operation() => _context.HasSucceeded.ShouldBeTrue();
}
