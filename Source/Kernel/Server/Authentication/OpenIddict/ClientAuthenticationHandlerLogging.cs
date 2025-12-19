// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

internal static partial class ClientAuthenticationHandlerLogging
{
    [LoggerMessage(LogLevel.Information, "ClientAuthenticationHandler: Handling token request for ClientId: {ClientId}")]
    internal static partial void HandlingTokenRequest(this ILogger<ClientAuthenticationHandler> logger, string? clientId);

    [LoggerMessage(LogLevel.Warning, "ClientAuthenticationHandler: Missing client credentials in token request")]
    internal static partial void MissingClientCredentials(this ILogger<ClientAuthenticationHandler> logger);

    [LoggerMessage(LogLevel.Warning, "ClientAuthenticationHandler: Application not found for ClientId: {ClientId}")]
    internal static partial void ApplicationNotFound(this ILogger<ClientAuthenticationHandler> logger, string clientId);

    [LoggerMessage(LogLevel.Information, "ClientAuthenticationHandler: Found application for ClientId: {ClientId}")]
    internal static partial void ApplicationFound(this ILogger<ClientAuthenticationHandler> logger, string clientId);

    [LoggerMessage(LogLevel.Warning, "ClientAuthenticationHandler: Secret verification failed for ClientId: {ClientId}")]
    internal static partial void SecretVerificationFailed(this ILogger<ClientAuthenticationHandler> logger, string clientId);

    [LoggerMessage(LogLevel.Information, "ClientAuthenticationHandler: Secret verification succeeded for ClientId: {ClientId}")]
    internal static partial void SecretVerificationSucceeded(this ILogger<ClientAuthenticationHandler> logger, string clientId);
}
