// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_the_host_registers_scheme_bound_policies_before : given.the_kernel_authorization
{
    void Establish()
    {
        _services.AddAuthorizationBuilder()
            .AddPolicy(WellKnownAuthorizationPolicies.Administrator, _ => _.AddAuthenticationSchemes("Bearer").RequireAuthenticatedUser().AddRequirements(new AdministratorRequirement()))
            .AddPolicy(WellKnownAuthorizationPolicies.AdministratorOperations, _ => _.AddAuthenticationSchemes("Bearer").RequireAuthenticatedUser().AddRequirements(new SystemStoreOperationRequirement()));
        _services.AddChronicleAdministratorAuthorization();
        BuildProvider();
    }

    [Fact] void should_keep_the_schemes_of_the_administrator_policy() => Options.GetPolicy(WellKnownAuthorizationPolicies.Administrator)!.AuthenticationSchemes.ShouldContainOnly("Bearer");
    [Fact] void should_keep_the_schemes_of_the_operations_policy() => Options.GetPolicy(WellKnownAuthorizationPolicies.AdministratorOperations)!.AuthenticationSchemes.ShouldContainOnly("Bearer");
    [Fact] void should_keep_the_authenticated_user_requirement() => Options.GetPolicy(WellKnownAuthorizationPolicies.Administrator)!.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement>().ShouldNotBeEmpty();
}
