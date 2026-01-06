// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Authentication;
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
        services.AddSingleton<IPasswordHasher<ChronicleUser>, ChroniclePasswordHasher>();

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
        var bearerScheme = usingExternalAuthority ? JwtBearerDefaults.AuthenticationScheme : OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;

        // Use a policy scheme that tries cookie first, then bearer token
        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = "MultiScheme";
            options.DefaultChallengeScheme = bearerScheme;
        })
        .AddPolicyScheme("MultiScheme", "Cookie or Bearer", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // If there's a cookie, use cookie authentication
                if (context.Request.Cookies.ContainsKey("Chronicle.Auth"))
                {
                    return IdentityConstants.ApplicationScheme;
                }

                // Otherwise use bearer token authentication
                return bearerScheme;
            };
        });

        if (usingExternalAuthority)
        {
            authBuilder.AddJwtBearer();
        }

        // Add cookie authentication for Identity API endpoints
        authBuilder.AddCookie(IdentityConstants.ApplicationScheme, options =>
        {
            options.Cookie.Name = "Chronicle.Auth";
            options.Cookie.HttpOnly = true;

            // In development without a certificate, allow non-secure cookies
            var hasSecureCertificate = !string.IsNullOrEmpty(chronicleOptions.Tls.CertificatePath);
#if DEVELOPMENT && DEBUG
            options.Cookie.SecurePolicy = hasSecureCertificate ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
#else
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
#endif
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
        });

        // Add claims transformation to include Chronicle-specific claims
        services.AddScoped<IClaimsTransformation, ChronicleClaimsTransformation>();

        services.AddAuthorizationBuilder()

            // Require authentication for all endpoints except those with [AllowAnonymous]
            // This applies zero-trust security across all gRPC services and HTTP endpoints
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }
}
