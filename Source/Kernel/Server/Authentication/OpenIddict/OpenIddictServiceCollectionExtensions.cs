// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// Extension methods for adding OpenIddict services conditionally.
/// </summary>
public static class OpenIddictServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenIddict services if the OAuth Authority feature is enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="chronicleOptions">The Chronicle options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenIddictIfEnabled(this IServiceCollection services, Configuration.ChronicleOptions chronicleOptions)
    {
        if (!chronicleOptions.Features.OAuthAuthority)
        {
            return services;
        }

        // Add Security storage implementations for OpenIddict
        services.AddSingleton<IApplicationStorage, ApplicationStorage>();
        services.AddSingleton<IAuthorizationStorage, AuthorizationStorage>();
        services.AddSingleton<IScopeStorage, ScopeStorage>();
        services.AddSingleton<ITokenStorage, TokenStorage>();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options
                    .AddApplicationStore<ApplicationStore>()
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
                options.SetTokenEndpointUris("/connect/token")
                    .AllowPasswordFlow()
                    .AllowClientCredentialsFlow()
                    .AllowRefreshTokenFlow()
                    .DisableAccessTokenEncryption()
                    .AddDevelopmentEncryptionCertificate()
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

        return services;
    }
}
