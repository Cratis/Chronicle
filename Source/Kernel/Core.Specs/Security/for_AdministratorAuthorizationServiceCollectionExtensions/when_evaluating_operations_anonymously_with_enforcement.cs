// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_evaluating_operations_anonymously_with_enforcement : given.the_kernel_authorization
{
    bool _operations;

    void Establish()
    {
        _services.AddChronicleAdministratorAuthorization();
        BuildProvider(enforceOperations: true);
    }

    async Task Because() => _operations = await AnonymousSucceeds(WellKnownAuthorizationPolicies.AdministratorOperations);

    [Fact] void should_fail() => _operations.ShouldBeFalse();
}
