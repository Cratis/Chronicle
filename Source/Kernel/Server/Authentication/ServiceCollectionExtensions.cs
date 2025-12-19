// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation;
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
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 4;
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
                    options.SetTokenEndpointUris("/connect/token");

                    options.AllowPasswordFlow();
                    options.AllowClientCredentialsFlow();
                    options.AllowRefreshTokenFlow();

                    options.DisableAccessTokenEncryption();

                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();

                    options.UseAspNetCore()
                           .EnableTokenEndpointPassthrough();

                    if (!string.IsNullOrWhiteSpace(chronicleOptions.Authentication.Authority))
                    {
                        options.SetIssuer(new Uri(chronicleOptions.Authentication.Authority));
                    }
                    else
                    {
                        var internalScheme = chronicleOptions.Tls.Disable ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;
                        var internalAuthority = new UriBuilder(internalScheme, "localhost", chronicleOptions.ManagementPort).Uri;
                        options.SetIssuer(internalAuthority);
                    }
                })
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore();

                    var authorityValue = chronicleOptions.Authentication.Authority;
                    string scheme;
                    string host;

                    if (!string.IsNullOrWhiteSpace(authorityValue))
                    {
                        var authorityUri = new Uri(authorityValue);
                        scheme = authorityUri.Scheme;
                        host = authorityUri.Host;
                    }
                    else
                    {
                        scheme = chronicleOptions.Tls.Disable ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;
                        host = "localhost";
                    }

                    var baseAuthority = (authorityValue ?? new UriBuilder(scheme, host, chronicleOptions.ManagementPort).Uri.ToString()).TrimEnd('/');

                    var issuers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        baseAuthority,
                        $"{baseAuthority}/"
                    };

                    var management = new UriBuilder(scheme, host, chronicleOptions.ManagementPort).Uri.ToString().TrimEnd('/');
                    var main = new UriBuilder(scheme, host, chronicleOptions.Port).Uri.ToString().TrimEnd('/');

                    issuers.Add(management);
                    issuers.Add($"{management}/");
                    issuers.Add(main);
                    issuers.Add($"{main}/");

                    options.Configure(o =>
                    {
                        o.TokenValidationParameters.ValidateIssuer = true;
                        o.TokenValidationParameters.ValidIssuers = issuers;
                        o.TokenValidationParameters.ValidateAudience = false;
                    });
                });
        }

        if (chronicleOptions.Authentication.Enabled)
        {
            var usingExternalAuthority = !string.IsNullOrEmpty(chronicleOptions.Authentication.Authority) && !chronicleOptions.Authentication.UseInternalAuthority;

            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = usingExternalAuthority ? JwtBearerDefaults.AuthenticationScheme : OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = usingExternalAuthority ? JwtBearerDefaults.AuthenticationScheme : OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });

            if (usingExternalAuthority)
            {
                authBuilder.AddJwtBearer(options =>
                {
                    options.Authority = chronicleOptions.Authentication.Authority;
                    var authorityUri = new Uri(chronicleOptions.Authentication.Authority);
                    var scheme = authorityUri.Scheme;
                    var host = authorityUri.Host;
                    var authority = chronicleOptions.Authentication.Authority.TrimEnd('/');

                    var allowedIssuers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        authority,
                        $"{authority}/"
                    };

                    var management = new UriBuilder(scheme, host, chronicleOptions.ManagementPort).Uri.ToString().TrimEnd('/');
                    var main = new UriBuilder(scheme, host, chronicleOptions.Port).Uri.ToString().TrimEnd('/');

                    allowedIssuers.Add(management);
                    allowedIssuers.Add($"{management}/");
                    allowedIssuers.Add(main);
                    allowedIssuers.Add($"{main}/");

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = true,
                        ValidAudience = "chronicle",
                        ValidIssuers = allowedIssuers
                    };
                });
            }

            services.AddAuthorizationBuilder()
                    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build());
        }

        return services;
    }
}
