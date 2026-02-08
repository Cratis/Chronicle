// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
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
    /// <exception cref="InvalidOperationException">Thrown if an encryption certificate is not configured in production.</exception>
    public static IServiceCollection AddChronicleAuthentication(this IServiceCollection services, Configuration.ChronicleOptions chronicleOptions)
    {
        services.AddSingleton<IUserStorage, UserStorage>();
        services.AddSingleton<IUserStore<User>, UserStore>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        // Configure Data Protection (required for webhook secret encryption)
        // This is set up here to ensure it's available even when OpenIddict is disabled
        services.AddSingleton<IXmlRepository, GrainBasedXmlRepository>();
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("Chronicle");

        // Configure key encryption with certificate if available
        var encryptionCert = chronicleOptions.EncryptionCertificate;
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
                "Configure 'EncryptionCertificate:CertificatePath' and 'EncryptionCertificate:CertificatePassword' " +
                "in your configuration. See the Chronicle documentation for more details on generating and configuring certificates.");
        }
#endif

        // Register webhook secret encryption service (depends on Data Protection)
        services.AddSingleton<Grains.Observation.Webhooks.IWebhookSecretEncryption, Storage.MongoDB.Observation.Webhooks.WebhookSecretEncryption>();

        // Add ASP.NET Identity
        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 4;
                options.User.RequireUniqueEmail = false;
            })
            .AddUserStore<UserStore>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        // Add OpenIdDict if OAuth Authority feature is enabled
        services.AddOpenIddictIfEnabled(chronicleOptions);

        var bearerScheme = chronicleOptions.Authentication.UseInternalAuthority ? OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme : JwtBearerDefaults.AuthenticationScheme;

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

        if (!chronicleOptions.Authentication.UseInternalAuthority)
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
#if DEVELOPMENT
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
