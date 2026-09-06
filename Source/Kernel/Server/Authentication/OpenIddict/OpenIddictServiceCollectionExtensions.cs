// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql.Cluster.Security;
using InMemoryAuthorizationStorage = Cratis.Chronicle.Storage.InMemory.Security.AuthorizationStorage;
using InMemoryScopeStorage = Cratis.Chronicle.Storage.InMemory.Security.ScopeStorage;
using InMemoryTokenStorage = Cratis.Chronicle.Storage.InMemory.Security.TokenStorage;
using MongoDbApplicationStorage = Cratis.Chronicle.Storage.MongoDB.Security.ApplicationStorage;
using MongoDbAuthorizationStorage = Cratis.Chronicle.Storage.MongoDB.Security.AuthorizationStorage;
using MongoDbScopeStorage = Cratis.Chronicle.Storage.MongoDB.Security.ScopeStorage;
using MongoDbTokenStorage = Cratis.Chronicle.Storage.MongoDB.Security.TokenStorage;

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
    /// <param name="encryptionCertificateRing">The <see cref="Cratis.Chronicle.Security.IEncryptionCertificateRing"/> holding the active and previous certificates.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required certificates are not configured in production.</exception>
    public static IServiceCollection AddOpenIddictIfEnabled(
        this IServiceCollection services,
        Configuration.ChronicleOptions chronicleOptions,
        Cratis.Chronicle.Security.IEncryptionCertificateRing encryptionCertificateRing)
    {
        // Disable OpenIddict if authentication is off entirely, if using an external authority, or if the
        // OAuthAuthority feature is disabled
        if (!chronicleOptions.Authentication.Enabled || !chronicleOptions.Features.OAuthAuthority || !chronicleOptions.Authentication.UseInternalAuthority)
        {
            return services;
        }

        // Add Security storage implementations for OpenIddict — use SQL, in-memory or MongoDB depending on storage type
        var isSqlStorage = string.Equals(chronicleOptions.Storage.Type, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase)
            || string.Equals(chronicleOptions.Storage.Type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase)
            || string.Equals(chronicleOptions.Storage.Type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase);
        var isInMemoryStorage = string.Equals(chronicleOptions.Storage.Type, StorageType.InMemory, StringComparison.OrdinalIgnoreCase);

        if (isSqlStorage)
        {
            services.AddSingleton(sp => sp.GetRequiredService<ISystemStorage>().Applications);
            services.AddSingleton<IAuthorizationStorage, SqlAuthorizationStorage>();
            services.AddSingleton<IScopeStorage, SqlScopeStorage>();
            services.AddSingleton<ITokenStorage, SqlTokenStorage>();
        }
        else if (isInMemoryStorage)
        {
            // The application store is shared with the seeded system storage so the internal client resolves;
            // tokens, scopes and authorizations are created at runtime and live in their own in-memory stores.
            services.AddSingleton(sp => sp.GetRequiredService<ISystemStorage>().Applications);
            services.AddSingleton<IAuthorizationStorage, InMemoryAuthorizationStorage>();
            services.AddSingleton<IScopeStorage, InMemoryScopeStorage>();
            services.AddSingleton<ITokenStorage, InMemoryTokenStorage>();
        }
        else
        {
            services.AddSingleton<IApplicationStorage, MongoDbApplicationStorage>();
            services.AddSingleton<IAuthorizationStorage, MongoDbAuthorizationStorage>();
            services.AddSingleton<IScopeStorage, MongoDbScopeStorage>();
            services.AddSingleton<ITokenStorage, MongoDbTokenStorage>();
        }

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
                // The whole certificate ring is registered, which is what lets tokens issued under a previous
                // certificate keep validating across a rotation. OpenIddict picks which one *issues* by its own
                // documented ordering - it prefers the X.509 key with the furthest expiration date, not the one
                // Chronicle marks active - so promoting a certificate that expires earlier than one still in the
                // ring leaves OpenIddict issuing under the longer-lived one.
                // Without a certificate, a DEVELOPMENT build uses ephemeral keys for convenience - the
                // same compile-time gate Program.cs uses to generate its self-signed TLS certificate.
                // The development container images set no environment variables, so a runtime
                // DOTNET_ENVIRONMENT / ASPNETCORE_ENVIRONMENT check leaves them on the production
                // branch and they abort at startup demanding a certificate. In production, a
                // certificate is required.
                if (encryptionCertificateRing.IsConfigured)
                {
                    var certificates = encryptionCertificateRing.All.Select(_ => _.Certificate).ToArray();
                    options.AddEncryptionCertificates(certificates)
                           .AddSigningCertificates(certificates);
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
                        "Configure 'EncryptionCertificate:CertificatePath' and 'EncryptionCertificate:CertificatePassword' " +
                        "in your configuration. See the Chronicle documentation for more details on generating and configuring certificates.");
                }
#endif

                // When no certificate is explicitly configured, development serves the token endpoint
                // with an auto-generated self-signed certificate. Relax OpenIddict's transport-security
                // requirement in that case so a proxy/forwarded-header setup that reports http still works.
                var hasExplicitCertificate = chronicleOptions.Tls.Enabled && !string.IsNullOrEmpty(chronicleOptions.Tls.CertificatePath);

                if (!hasExplicitCertificate)
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
                    // The Chronicle port always serves TLS, so the token endpoint (and therefore the
                    // issuer) is always https.
                    var internalAuthority = new UriBuilder(Uri.UriSchemeHttps, "localhost", chronicleOptions.Port).Uri;
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
                    // The Chronicle port always serves TLS, so tokens are issued and validated over https.
                    scheme = Uri.UriSchemeHttps;
                    host = "localhost";
                }

                var baseAuthority = (authorityValue ?? new UriBuilder(scheme, host, chronicleOptions.Port).Uri.ToString()).TrimEnd('/');

                var issuers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                        baseAuthority,
                        $"{baseAuthority}/"
                };

                var main = new UriBuilder(scheme, host, chronicleOptions.Port).Uri.ToString().TrimEnd('/');

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
