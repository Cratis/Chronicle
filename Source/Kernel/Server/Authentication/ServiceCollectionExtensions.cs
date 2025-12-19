// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;
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

        // Add Security storage implementations for OpenIddict
        services.AddSingleton<IApplicationStorage, ApplicationStorage>();
        services.AddSingleton<IAuthorizationStorage, AuthorizationStorage>();
        services.AddSingleton<IScopeStorage, ScopeStorage>();
        services.AddSingleton<ITokenStorage, TokenStorage>();

        // Add ASP.NET Identity
        services.AddIdentityCore<ChronicleUser>(options =>
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
        .AddSignInManager()
        .AddDefaultTokenProviders();

        // Add OpenIdDict if OAuth Authority feature is enabled
        if (chronicleOptions.Features.OAuthAuthority)
        {
            services.AddOpenIddict()
                .AddCore(options =>
                {
                    // Use Chronicle's storage with proper OpenIddict stores
                    options.AddApplicationStore<ApplicationStore>()
                        .AddAuthorizationStore<AuthorizationStore>()
                        .AddScopeStore<ScopeStore>()
                        .AddTokenStore<TokenStore>()
                        .SetDefaultApplicationEntity<Application>()
                        .SetDefaultAuthorizationEntity<Authorization>()
                        .SetDefaultScopeEntity<Scope>()
                        .SetDefaultTokenEntity<Token>();
                })
                .AddServer(options =>
                {
                    // Enable the token endpoint
                    options.SetTokenEndpointUris("/connect/token");

                    // Enable flows
                    options.AllowPasswordFlow();
                    options.AllowClientCredentialsFlow();
                    options.AllowRefreshTokenFlow();

                    // Disable access token encryption for development
                    options.DisableAccessTokenEncryption();

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
