// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_a_non_administrator_uses_management_in_audit_mode : given.chronicle_authentication_services
{
    bool _allowed;

    async Task Because()
    {
        var authenticationServices = BuildServices(
            authenticationEnabled: true,
            configureServices: services =>
            {
                services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions()));
                services.AddScoped(_ => Substitute.For<IAdministratorAuthorization>());
            });
        await using var provider = authenticationServices.ServiceProvider;
        await using var scope = provider.CreateAsyncScope();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "Bearer"));
        _allowed = (await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(principal, new object(), WellKnownAuthorizationPolicies.Administrator)).Succeeded;
    }

    [Fact] void should_enforce_tier_a_even_though_operational_enforcement_defaults_off() => _allowed.ShouldBeFalse();
}
