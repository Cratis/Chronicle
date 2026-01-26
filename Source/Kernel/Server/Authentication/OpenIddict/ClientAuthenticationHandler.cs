// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Cratis.Infrastructure.Security;
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
            .SetOrder(int.MinValue + 100000) // Run very early in validation phase
            .SetType(OpenIddictServerHandlerType.BuiltIn)
            .Build();

    /// <summary>
    /// Handles the token request validation by authenticating client credentials.
    /// </summary>
    /// <param name="context">The token request validation context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask HandleAsync(ValidateTokenRequestContext context)
    {
        if (context.Request.IsClientCredentialsGrantType())
        {
            var clientId = context.Request.ClientId;
            var clientSecret = context.Request.ClientSecret;

            logger.HandlingTokenRequest(clientId);

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                logger.MissingClientCredentials();
                context.Reject(
                    error: OpenIddictConstants.Errors.InvalidClient,
                    description: "The client credentials are missing.",
                    uri: "https://documentation.openiddict.com/errors/ID2055");
                return;
            }

            var application = await applicationStorage.GetByClientId(clientId);
            if (application is null)
            {
                logger.ApplicationNotFound(clientId);
                context.Reject(
                    error: OpenIddictConstants.Errors.InvalidClient,
                    description: "The specified client credentials are invalid.",
                    uri: "https://documentation.openiddict.com/errors/ID2055");
                return;
            }

            logger.ApplicationFound(clientId);

            if (application.ClientSecret is null || !HashHelper.Verify(clientSecret, application.ClientSecret.Value))
            {
                logger.SecretVerificationFailed(clientId);
                context.Reject(
                    error: OpenIddictConstants.Errors.InvalidClient,
                    description: "The specified client credentials are invalid.",
                    uri: "https://documentation.openiddict.com/errors/ID2055");
                return;
            }

            logger.SecretVerificationSucceeded(clientId);
        }
        else
        {
            logger.NotClientCredentialsGrant(context.Request.GrantType ?? string.Empty);
        }
    }
}
