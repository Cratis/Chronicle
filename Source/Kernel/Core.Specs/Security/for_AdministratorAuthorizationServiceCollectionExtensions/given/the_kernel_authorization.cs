// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions.given;

public class the_kernel_authorization : Specification
{
    protected ServiceCollection _services;
    protected ServiceProvider _provider;

    protected AuthorizationOptions Options => _provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

    protected static bool HasRequirement<TRequirement>(AuthorizationPolicy? policy) => policy?.Requirements.OfType<TRequirement>().Any() == true;

    protected async Task<bool> AnonymousSucceeds(string policy)
    {
        await using var scope = _provider.CreateAsyncScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var result = await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), new object(), policy);
        return result.Succeeded;
    }

    protected void BuildProvider(bool enforceOperations = false)
    {
        _services.AddLogging();
        _services.AddSingleton<IOptions<ChronicleOptions>>(Microsoft.Extensions.Options.Options.Create(new ChronicleOptions
        {
            Authentication = new Authentication { EnforceAdministratorPolicyForOperations = enforceOperations }
        }));
        _services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
        _provider = _services.BuildServiceProvider();
    }

    void Establish() => _services = [];

    void Destroy() => _provider?.Dispose();
}
