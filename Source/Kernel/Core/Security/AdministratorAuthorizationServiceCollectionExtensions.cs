// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Extension methods for adding the kernel's administrator authorization to a service collection.
/// </summary>
public static class AdministratorAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the administrator authorization the kernel's services depend on: the check, its handlers and the policies they serve.
    /// </summary>
    /// <remarks>
    /// The policies are requirement-only and are added only when the host has not registered them; a host that registered
    /// them (for example with authentication schemes) keeps its own. A same-named policy that lacks the kernel's requirement
    /// is rejected with <see cref="KernelAuthorizationPolicyNameIsReserved"/> when the authorization options are resolved.
    /// No default or fallback policy is set, so nothing outside the kernel's own policies is affected.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChronicleAdministratorAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationCore();
        services.AddHttpContextAccessor();
        services.TryAddScoped<IAdministratorAuthorization, AdministratorAuthorization>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, AdministratorAuthorizationHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, SystemStoreOperationAuthorizationHandler>());
        services.PostConfigure<AuthorizationOptions>(options =>
        {
            EnsurePolicy<AdministratorRequirement>(options, WellKnownAuthorizationPolicies.Administrator);
            EnsurePolicy<SystemStoreOperationRequirement>(options, WellKnownAuthorizationPolicies.AdministratorOperations);
        });
        return services;
    }

    static void EnsurePolicy<TRequirement>(AuthorizationOptions options, string name)
        where TRequirement : IAuthorizationRequirement, new()
    {
        var existing = options.GetPolicy(name);
        if (existing is null)
        {
            options.AddPolicy(name, policy => policy.AddRequirements(new TRequirement()));
            return;
        }

        if (!existing.Requirements.OfType<TRequirement>().Any())
        {
            throw new KernelAuthorizationPolicyNameIsReserved(name);
        }
    }
}
