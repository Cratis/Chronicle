// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_registered_twice : given.the_kernel_authorization
{
    void Establish()
    {
        _services.AddChronicleAdministratorAuthorization();
        _services.AddChronicleAdministratorAuthorization();
        BuildProvider();
    }

    [Fact] void should_register_each_handler_once() => _services.Count(_ => _.ServiceType == typeof(IAuthorizationHandler) && _.ImplementationType == typeof(AdministratorAuthorizationHandler)).ShouldEqual(1);
    [Fact] void should_register_the_authorization_once() => _services.Count(_ => _.ServiceType == typeof(IAdministratorAuthorization)).ShouldEqual(1);
    [Fact] void should_keep_the_policies() => HasRequirement<AdministratorRequirement>(Options.GetPolicy(WellKnownAuthorizationPolicies.Administrator)).ShouldBeTrue();
}
