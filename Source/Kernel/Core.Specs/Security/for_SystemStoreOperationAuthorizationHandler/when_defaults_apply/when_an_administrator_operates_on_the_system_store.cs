// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.when_defaults_apply;

public class when_an_administrator_operates_on_the_system_store : given.an_operation_policy
{
    void Establish()
    {
        _useConfigurationDefaults = true;
        _authorization.IsAuthorized(_principal).Returns(true);
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_operation() => _context.HasSucceeded.ShouldBeTrue();
}
