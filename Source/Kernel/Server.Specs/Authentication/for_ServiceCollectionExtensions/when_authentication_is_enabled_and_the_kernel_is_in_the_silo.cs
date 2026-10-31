// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_authentication_is_enabled_and_the_kernel_is_in_the_silo : given.chronicle_authentication_services
{
    AuthorizationOptions _options;
    Exception _result;

    void Because()
    {
        var authenticationServices = BuildServices(
            authenticationEnabled: true,
            configureServices: services =>
            {
                services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions()));
                services.AddSingleton(Substitute.For<IAdministratorPrivileges>());

                // The kernel in the silo registers the same authorization again, after the server's own policies.
                services.AddChronicleAdministratorAuthorization();
            });
        using var provider = authenticationServices.ServiceProvider;
        _result = Catch.Exception(() => _options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value);
    }

    [Fact] void should_not_reject_the_server_policies() => _result.ShouldBeNull();
    [Fact] void should_keep_the_schemes_of_the_administrator_policy() => _options.GetPolicy(WellKnownAuthorizationPolicies.Administrator)!.AuthenticationSchemes.ShouldContain("MultiScheme");
    [Fact] void should_keep_the_schemes_of_the_operations_policy() => _options.GetPolicy(WellKnownAuthorizationPolicies.AdministratorOperations)!.AuthenticationSchemes.ShouldContain("MultiScheme");
    [Fact] void should_require_an_authenticated_user_for_administration() => _options.GetPolicy(WellKnownAuthorizationPolicies.Administrator)!.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldNotBeEmpty();
    [Fact] void should_require_an_authenticated_user_for_operations() => _options.GetPolicy(WellKnownAuthorizationPolicies.AdministratorOperations)!.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldNotBeEmpty();
    [Fact] void should_require_authentication_in_the_fallback_policy() => _options.FallbackPolicy!.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldNotBeEmpty();
}
