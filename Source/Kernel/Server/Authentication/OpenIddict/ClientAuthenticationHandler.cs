// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// Handles client authentication for OpenIddict.
/// </summary>
/// <param name="applicationStorage">The application storage.</param>
/// <param name="logger">The logger.</param>
public class ClientAuthenticationHandler(
    IApplicationStorage applicationStorage,
    ILogger<ClientAuthenticationHandler> logger) : IOpenIddictServerHandler<ValidateTokenRequestContext>
{
    /// <summary>
    /// Gets the descriptor for this handler.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateTokenRequestContext>()
            .UseSingletonHandler<ClientAuthenticationHandler>()
            .SetOrder(int.MinValue + 100000)  // Run very early in validation phase
            .SetType(OpenIddictServerHandlerType.BuiltIn)
            .Build();

    /// <summary>
    /// Handles the token request validation by authenticating client credentials.
    /// </summary>
    /// <param name="context">The token request validation context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask HandleAsync(ValidateTokenRequestContext context)
    {
        Console.WriteLine("⚠️⚠️⚠️ ClientAuthenticationHandler.HandleAsync() CALLED ⚠️⚠️⚠️");

        if (context.Request.IsClientCredentialsGrantType())
        {
            Console.WriteLine("⚠️⚠️⚠️ IS CLIENT_CREDENTIALS GRANT ⚠️⚠️⚠️");

            var clientId = context.Request.ClientId;
            var clientSecret = context.Request.ClientSecret;

            logger.HandlingTokenRequest(clientId);
            Console.WriteLine($"⚠️⚠️⚠️ ClientId: {clientId}, ClientSecret: {clientSecret?.Substring(0, Math.Min(10, clientSecret?.Length ?? 0))}... ⚠️⚠️⚠️");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("⚠️⚠️⚠️ MISSING CREDENTIALS - REJECTING ⚠️⚠️⚠️");
                logger.MissingClientCredentials();
                context.Reject(
                    error: OpenIddictConstants.Errors.InvalidClient,
                    description: "The client credentials are missing.",
                    uri: "https://documentation.openiddict.com/errors/ID2055");
                return;
            }

            var application = await applicationStorage.GetByClientId(clientId);
            if (application == null)
            {
                Console.WriteLine($"⚠️⚠️⚠️ APPLICATION NOT FOUND for {clientId} - REJECTING ⚠️⚠️⚠️");
                logger.ApplicationNotFound(clientId);
                context.Reject(
                    error: OpenIddictConstants.Errors.InvalidClient,
                    description: "The specified client credentials are invalid.",
                    uri: "https://documentation.openiddict.com/errors/ID2055");
                return;
            }

            logger.ApplicationFound(clientId);
            Console.WriteLine($"⚠️⚠️⚠️ APPLICATION FOUND for {clientId}, HashedSecret: {application.ClientSecret?.Substring(0, Math.Min(20, application.ClientSecret?.Length ?? 0))}... ⚠️⚠️⚠️");

            if (application.ClientSecret == null || !VerifySecret(clientSecret, application.ClientSecret))
            {
                Console.WriteLine($"⚠️⚠️⚠️ SECRET VERIFICATION FAILED for {clientId} - REJECTING ⚠️⚠️⚠️");
                logger.SecretVerificationFailed(clientId);
                context.Reject(
                    error: OpenIddictConstants.Errors.InvalidClient,
                    description: "The specified client credentials are invalid.",
                    uri: "https://documentation.openiddict.com/errors/ID2055");
                return;
            }

            Console.WriteLine($"⚠️⚠️⚠️ SECRET VERIFICATION SUCCEEDED for {clientId} ⚠️⚠️⚠️");
            logger.SecretVerificationSucceeded(clientId);
        }
        else
        {
            Console.WriteLine($"⚠️⚠️⚠️ NOT CLIENT_CREDENTIALS GRANT: {context.Request.GrantType} ⚠️⚠️⚠️");
        }
    }

    static bool VerifySecret(string secret, string hashedSecret)
    {
        var parts = hashedSecret.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        var testHash = KeyDerivation.Pbkdf2(
            password: secret,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        return hash.SequenceEqual(testHash);
    }
}
