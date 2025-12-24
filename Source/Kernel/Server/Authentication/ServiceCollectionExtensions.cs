// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Validation.AspNetCore;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Extension methods for adding authentication to the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Chronicle authentication services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="chronicleOptions">The Chronicle options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChronicleAuthentication(this IServiceCollection services, Configuration.ChronicleOptions chronicleOptions)
    {
        if (!chronicleOptions.Authentication.Enabled)
        {
            return services;
        }

        services.AddSingleton<IUserStorage, UserStorage>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IUserStore<ChronicleUser>, ChronicleUserStore>();

        // Add ASP.NET Identity
        services.AddIdentityCore<ChronicleUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 4;
                options.User.RequireUniqueEmail = false;
            })
            .AddUserStore<ChronicleUserStore>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        // Add OpenIdDict if OAuth Authority feature is enabled
        services.AddOpenIddictIfEnabled(chronicleOptions);

        var usingExternalAuthority = !string.IsNullOrEmpty(chronicleOptions.Authentication.Authority) && !chronicleOptions.Authentication.UseInternalAuthority;

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = usingExternalAuthority ? JwtBearerDefaults.AuthenticationScheme : OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = usingExternalAuthority ? JwtBearerDefaults.AuthenticationScheme : OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        if (usingExternalAuthority)
        {
            authBuilder.AddJwtBearer();
        }

        services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build());

        return services;
    }
}
