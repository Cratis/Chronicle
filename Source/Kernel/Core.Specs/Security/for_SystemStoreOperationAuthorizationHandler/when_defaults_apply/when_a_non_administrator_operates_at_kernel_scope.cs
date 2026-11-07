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

    [Fact] void should_allow_the_operation_in_audit_mode() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_warn_about_the_denial() => _logger.Entries.Count.ShouldEqual(1);
}
