// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_evaluating_operations_anonymously_with_defaults : given.the_kernel_authorization
{
    bool _operations;
    bool _administrator;

    void Establish()
    {
        _services.AddChronicleAdministratorAuthorization();
        BuildProvider();
    }

    async Task Because()
    {
        _operations = await AnonymousSucceeds(WellKnownAuthorizationPolicies.AdministratorOperations);
        _administrator = await AnonymousSucceeds(WellKnownAuthorizationPolicies.Administrator);
    }

    [Fact] void should_succeed_for_operations_in_audit_mode() => _operations.ShouldBeTrue();
    [Fact] void should_deny_administration() => _administrator.ShouldBeFalse();
}
