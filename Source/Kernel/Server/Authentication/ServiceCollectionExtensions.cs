// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Users;
using Cratis.Chronicle.Storage.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
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
        services.AddSingleton<IUserStorage, UserStorage>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IUserStore<ChronicleUser>, ChronicleUserStore>();

        // Add ASP.NET Identity
        services.AddIdentity<ChronicleUser, IdentityRole>(options =>
        {
            // Configure identity options
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 4; // Minimum for admin user
            options.User.RequireUniqueEmail = false;
        })
        .AddUserStore<ChronicleUserStore>()
        .AddDefaultTokenProviders();

        // Add OpenIdDict if OAuth Authority feature is enabled
        if (chronicleOptions.Features.OAuthAuthority)
        {
            services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseMongoDb();
                })
                .AddServer(options =>
                {
                    // Enable the token endpoint
                    options.SetTokenEndpointUris("/connect/token");

                    // Enable the password flow
                    options.AllowPasswordFlow();
                    options.AllowRefreshTokenFlow();

                    // Accept anonymous clients (clients without a client_id)
                    options.AcceptAnonymousClients();

                    // Register the signing and encryption credentials
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();

                    // Register the ASP.NET Core host
                    options.UseAspNetCore()
                           .EnableTokenEndpointPassthrough();
                })
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });
        }

        // Add authentication if enabled
        if (chronicleOptions.Authentication.Enabled)
        {
            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });

            // If external authority is configured, add JWT bearer
            if (!string.IsNullOrEmpty(chronicleOptions.Authentication.Authority) && !chronicleOptions.Authentication.UseInternalAuthority)
            {
                authBuilder.AddJwtBearer(options =>
                {
                    options.Authority = chronicleOptions.Authentication.Authority;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidAudience = "chronicle"
                    };
                });
            }
        }

        return services;
    }
}
