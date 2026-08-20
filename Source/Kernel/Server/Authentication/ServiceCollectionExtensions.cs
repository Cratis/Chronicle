// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Cratis.Chronicle.Storage;
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
        var isSqlStorage = string.Equals(chronicleOptions.Storage.Type, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase)
            || string.Equals(chronicleOptions.Storage.Type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase)
            || string.Equals(chronicleOptions.Storage.Type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase);
        var isInMemoryStorage = string.Equals(chronicleOptions.Storage.Type, StorageType.InMemory, StringComparison.OrdinalIgnoreCase);

        // SQL and in-memory back users through the system storage; MongoDB uses its own user storage.
        if (isSqlStorage || isInMemoryStorage)
            services.AddSingleton(sp => sp.GetRequiredService<ISystemStorage>().Users);
        else
            services.AddSingleton<IUserStorage, UserStorage>();
        services.AddSingleton<IUserStore<User>, UserStore>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        // The ring is resolved once, here, because Data Protection and OpenIddict are configured while the
        // service collection is still being built. Registering that same instance is what makes the rotation
        // diagnostic report the certificates those two were actually handed, rather than a second reading of
        // the same configuration.
        var encryptionCertificateRing = Cratis.Chronicle.Security.EncryptionCertificateRing.From(chronicleOptions);
        services.AddSingleton<Cratis.Chronicle.Security.IEncryptionCertificateRing>(encryptionCertificateRing);

        // Configure Data Protection (required for webhook secret encryption)
        // This is set up here to ensure it's available even when OpenIddict is disabled
        services.AddSingleton<IXmlRepository, GrainBasedXmlRepository>();
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("Chronicle");

        // New keys are protected with the active certificate; every certificate in the ring can unprotect,
        // so keys written before a rotation stay readable through it. The active certificate is passed to
        // both calls deliberately - the decryption set is stated in full rather than relying on
        // ProtectKeysWithCertificate to also register it.
        if (encryptionCertificateRing.IsConfigured)
        {
            dataProtectionBuilder
                .ProtectKeysWithCertificate(encryptionCertificateRing.Active.Certificate)
                .UnprotectKeysWithAnyCertificate([.. encryptionCertificateRing.All.Select(_ => _.Certificate)]);
        }

        // Everything from here on exists to authenticate callers. With authentication off there is nothing to
        // authenticate them against, so the token authority, the identity stack and the schemes are all skipped
        // and the fallback policy lets every request through. Data Protection above stays either way - webhook
        // secret encryption depends on it and has nothing to do with authentication.
        if (!chronicleOptions.Authentication.Enabled)
        {
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true)
                    .Build());

            return services;
        }

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
        services.AddOpenIddictIfEnabled(chronicleOptions, encryptionCertificateRing);

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
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
