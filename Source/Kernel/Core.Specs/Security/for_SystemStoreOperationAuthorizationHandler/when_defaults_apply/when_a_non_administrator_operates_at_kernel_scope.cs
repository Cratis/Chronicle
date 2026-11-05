// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.when_defaults_apply;

public class when_a_non_administrator_operates_at_kernel_scope : given.an_operation_policy
{
    void Establish()
    {
        _useConfigurationDefaults = true;
        _operation = new RemoveApplication(Guid.NewGuid());
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_refuse_the_operation() => _context.HasSucceeded.ShouldBeFalse();
}
