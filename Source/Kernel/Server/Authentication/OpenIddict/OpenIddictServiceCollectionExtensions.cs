// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
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
    /// <exception cref="InvalidOperationException">Thrown when required certificates are not configured in production.</exception>
    public static IServiceCollection AddOpenIddictIfEnabled(this IServiceCollection services, Configuration.ChronicleOptions chronicleOptions)
    {
        // Disable OpenIddict if using an external authority or if OAuthAuthority feature is disabled
        if (!chronicleOptions.Features.OAuthAuthority || !chronicleOptions.Authentication.UseInternalAuthority)
        {
            return services;
        }

        // Add Security storage implementations for OpenIddict
        services.AddSingleton<IApplicationStorage, ApplicationStorage>();
        services.AddSingleton<IAuthorizationStorage, AuthorizationStorage>();
        services.AddSingleton<IScopeStorage, ScopeStorage>();
        services.AddSingleton<ITokenStorage, TokenStorage>();

        // Note: Data Protection is configured in AddChronicleAuthentication
        // and will be reused here for OpenIddict
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options
                    .ReplaceApplicationStore<Application, ApplicationStore>(ServiceLifetime.Singleton)
                    .ReplaceAuthorizationStore<Authorization, AuthorizationStore>(ServiceLifetime.Singleton)
                    .ReplaceScopeStore<Scope, ScopeStore>(ServiceLifetime.Singleton)
                    .ReplaceTokenStore<Token, TokenStore>(ServiceLifetime.Singleton)
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
                    .AcceptAnonymousClients()
                    .DisableAccessTokenEncryption()
                    .UseDataProtection();

                // Configure encryption and signing keys
                // If a certificate is configured, use it for encryption and signing
                // In development without a certificate, use ephemeral keys for convenience
                // In production, a certificate is required
                var encryptionCertificate = chronicleOptions.EncryptionCertificate;
                if (encryptionCertificate.IsConfigured && File.Exists(encryptionCertificate.CertificatePath))
                {
                    var cert = X509CertificateLoader.LoadPkcs12FromFile(
                        encryptionCertificate.CertificatePath,
                        encryptionCertificate.CertificatePassword);
                    options.AddEncryptionCertificate(cert)
                           .AddSigningCertificate(cert);
                }
                else if (IsDevelopmentEnvironment())
                {
                    options.AddEphemeralEncryptionKey()
                           .AddEphemeralSigningKey();
                }
                else
                {
                    throw new InvalidOperationException(
                        "An encryption certificate is required in production for OpenIddict key security. " +
                        "Configure 'EncryptionCertificate:CertificatePath' and 'EncryptionCertificate:CertificatePassword' " +
                        "in your configuration. See the Chronicle documentation for more details on generating and configuring certificates.");
                }

                // Determine if the identity provider has TLS enabled (token endpoint runs on the management port).
                // Prefer IdentityProvider:Certificate when configured and fall back to top-level Tls for backward compatibility.
                var identityProviderCertificate = chronicleOptions.IdentityProvider?.Certificate ?? chronicleOptions.Tls;
                var identityProviderHasSecureCertificate = identityProviderCertificate.Enabled && !string.IsNullOrEmpty(identityProviderCertificate.CertificatePath);

                // Allow HTTP connections when Workbench TLS is disabled (e.g. behind ingress/reverse proxy)
                if (!identityProviderHasSecureCertificate)
                {
                    options.UseAspNetCore()
                           .EnableTokenEndpointPassthrough()
                           .DisableTransportSecurityRequirement();
                }
                else
                {
                    options.UseAspNetCore()
                           .EnableTokenEndpointPassthrough();
                }

                if (!string.IsNullOrWhiteSpace(chronicleOptions.Authentication.Authority))
                {
                    options.SetIssuer(new Uri(chronicleOptions.Authentication.Authority));
                }
                else
                {
                    // Use identity provider certificate config to determine the issuer scheme
                    // since the token endpoint is served on the management port
                    var internalScheme = identityProviderHasSecureCertificate ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
                    var internalAuthority = new UriBuilder(internalScheme, "localhost", chronicleOptions.ManagementPort).Uri;
                    options.SetIssuer(internalAuthority);
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
                options.UseDataProtection();

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
                    // Use identity provider certificate config since tokens are served on the management port.
                    // Prefer IdentityProvider:Certificate when configured and fall back to top-level Tls.
                    var validationIdentityProviderCertificate = chronicleOptions.IdentityProvider?.Certificate ?? chronicleOptions.Tls;
                    var validationHasSecureCertificate = validationIdentityProviderCertificate.Enabled && !string.IsNullOrEmpty(validationIdentityProviderCertificate.CertificatePath);
                    scheme = validationHasSecureCertificate ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
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

                    // TODO: Re-enable audience validation when we have a way to set audiences on tokens
#pragma warning disable CA5404 // Do not disable token validation checks
                    o.TokenValidationParameters.ValidateAudience = false;
#pragma warning restore CA5404 // Do not disable token validation checks
                });
            });

        return services;
    }

    static bool IsDevelopmentEnvironment()
    {
        var dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var aspnetcoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(dotnetEnvironment, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(aspnetcoreEnvironment, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
