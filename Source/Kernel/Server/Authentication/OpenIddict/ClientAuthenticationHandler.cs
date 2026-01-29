// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Grains.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

#if DEVELOPMENT

/// <summary>
/// Handles client authentication for OpenIddict during ProcessAuthentication phase.
/// This handler validates client secrets using ASP.NET Core PasswordHasher and clears the
/// secret from the request to prevent OpenIddict's built-in PBKDF2 validation from running.
/// </summary>
/// <param name="applicationStorage">The application storage.</param>
/// <param name="passwordHasher">The password hasher for verifying client secrets.</param>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="logger">The logger.</param>
public class ClientAuthenticationHandler(
    IApplicationStorage applicationStorage,
    IPasswordHasher<object> passwordHasher,
    IGrainFactory grainFactory,
    ILogger<ClientAuthenticationHandler> logger) : IOpenIddictServerHandler<ProcessAuthenticationContext>
{
    /// <summary>
    /// Gets the descriptor for this handler.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessAuthenticationContext>()
            .AddFilter<OpenIddictServerHandlerFilters.RequireClientIdParameter>()
            .AddFilter<OpenIddictServerHandlerFilters.RequireClientSecretParameter>()
            .UseScopedHandler<ClientAuthenticationHandler>()

            // Run before OpenIddict's built-in ValidateClientSecret which is at ValidateClientType.Order + 1_000
            // ValidateClientType is at ValidateClientId.Order + 1_000
            // ValidateClientId is at ValidateClientAssertionAudience.Order + 1_000
            // So we run just before ValidateClientSecret
            .SetOrder(OpenIddictServerHandlers.ValidateClientType.Descriptor.Order + 500)
            .SetType(OpenIddictServerHandlerType.BuiltIn)
            .Build();

    /// <summary>
    /// Handles the authentication by validating client credentials using Chronicle's hashing.
    /// </summary>
    /// <param name="context">The process authentication context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask HandleAsync(ProcessAuthenticationContext context)
    {
        var clientId = context.ClientId;
        var clientSecret = context.ClientSecret;

        logger.HandlingTokenRequest(clientId);

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            // Let other handlers deal with missing credentials
            return;
        }

        var application = await applicationStorage.GetByClientId(clientId);
        if (application is null)
        {
            logger.ApplicationNotFound(clientId);

            // Append event for unknown application login attempt
            var unknownAppEvent = new UnknownApplicationLoginAttempted(clientId);
            var eventSequence = grainFactory.GetEventLog();
            await eventSequence.Append(Guid.Empty, unknownAppEvent);

            context.Reject(
                error: OpenIddictConstants.Errors.InvalidClient,
                description: "The specified client credentials are invalid.",
                uri: "https://documentation.openiddict.com/errors/ID2055");
            return;
        }

        logger.ApplicationFound(clientId);

        var verificationResult = passwordHasher.VerifyHashedPassword(
            new object(),
            application.ClientSecret?.Value ?? string.Empty,
            clientSecret);

        if (application.ClientSecret is null || verificationResult == PasswordVerificationResult.Failed)
        {
            logger.SecretVerificationFailed(clientId);

            // Append event for invalid application credentials
            var invalidAppCredsEvent = new InvalidApplicationCredentialsProvided(clientId);
            var invalidEventSequence = grainFactory.GetEventLog();
            await invalidEventSequence.Append(application.Id.Value, invalidAppCredsEvent);

            context.Reject(
                error: OpenIddictConstants.Errors.InvalidClient,
                description: "The specified client credentials are invalid.",
                uri: "https://documentation.openiddict.com/errors/ID2055");
            return;
        }

        logger.SecretVerificationSucceeded(clientId);

        // Clear the client secret from the request to prevent OpenIddict's built-in
        // ValidateClientSecret handler from running. The RequireClientSecretParameter
        // filter checks context.Transaction.Request.ClientSecret, so clearing it
        // will skip the built-in PBKDF2-based validation.
        context.Transaction.Request!.ClientSecret = null;
    }
}

#endif
