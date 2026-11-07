// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.when_defaults_apply;

public class when_an_application_operates_on_its_own_store : given.an_operation_policy
{
    void Establish()
    {
        _useConfigurationDefaults = true;
        _operation = new ReplayObserver("application", "default", "observer", string.Empty);
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_operation_without_administrator_privilege() => _context.HasSucceeded.ShouldBeTrue();
}
