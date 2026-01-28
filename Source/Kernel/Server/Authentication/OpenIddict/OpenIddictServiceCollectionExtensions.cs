// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;

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
    /// <exception cref="InvalidOperationException">If not development environment this exception will be thrown when the encryption certificate is not configured.</exception>
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

        // Configure Data Protection with grain-based key storage for multi-instance support
        services.AddSingleton<IXmlRepository, GrainBasedXmlRepository>();
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("Chronicle");

        // Configure key encryption with certificate
        // In production, this certificate is required for secure key storage
        // In development, it is optional for convenience
        var encryptionCert = chronicleOptions.Authentication.EncryptionCertificate;
        if (encryptionCert.IsConfigured && File.Exists(encryptionCert.CertificatePath))
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                encryptionCert.CertificatePath,
                encryptionCert.CertificatePassword);
            dataProtectionBuilder.ProtectKeysWithCertificate(certificate);
        }
#if !DEVELOPMENT
        else
        {
            throw new InvalidOperationException(
                "An encryption certificate is required in production for Data Protection key security. " +
                "Configure 'Authentication:EncryptionCertificate:CertificatePath' and 'Authentication:EncryptionCertificate:CertificatePassword' " +
                "in your configuration. See the Chronicle documentation for more details on generating and configuring certificates.");
        }
#endif

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
                var encryptionCertificate = chronicleOptions.Authentication.EncryptionCertificate;
                if (encryptionCertificate.IsConfigured && File.Exists(encryptionCertificate.CertificatePath))
                {
                    var cert = X509CertificateLoader.LoadPkcs12FromFile(
                        encryptionCertificate.CertificatePath!,
                        encryptionCertificate.CertificatePassword);
                    options.AddEncryptionCertificate(cert)
                           .AddSigningCertificate(cert);
                }
#if DEVELOPMENT
                else
                {
                    options.AddEphemeralEncryptionKey()
                           .AddEphemeralSigningKey();
                }
#else
                else
                {
                    throw new InvalidOperationException(
                        "An encryption certificate is required in production for OpenIddict key security. " +
                        "Configure 'Authentication:EncryptionCertificate:CertificatePath' and 'Authentication:EncryptionCertificate:CertificatePassword' " +
                        "in your configuration. See the Chronicle documentation for more details on generating and configuring certificates.");
                }
#endif

                // Determine if we have a secure certificate configured
                var hasSecureCertificate = !string.IsNullOrEmpty(chronicleOptions.Tls.CertificatePath);

                // In development without a certificate, allow HTTP connections
#if DEVELOPMENT
                if (!hasSecureCertificate)
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
#else
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough();
#endif

                if (!string.IsNullOrWhiteSpace(chronicleOptions.Authentication.Authority))
                {
                    options.SetIssuer(new Uri(chronicleOptions.Authentication.Authority));
                }
                else
                {
                    // In development without a certificate, use HTTP; otherwise use HTTPS
#if DEVELOPMENT
                    var internalScheme = hasSecureCertificate ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
#else
                    var internalScheme = Uri.UriSchemeHttps;
#endif
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
                    // In development without a certificate, use HTTP; otherwise use HTTPS
                    var hasSecureCertificate = !string.IsNullOrEmpty(chronicleOptions.Tls.CertificatePath);
#if DEVELOPMENT
                    scheme = hasSecureCertificate ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
#else
                    scheme = Uri.UriSchemeHttps;
#endif
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
}
