// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.Controllers;

internal static partial class AuthorizationControllerLogging
{
    [LoggerMessage(LogLevel.Debug, "Token endpoint called with grant type: {GrantType}")]
    internal static partial void TokenEndpointCalled(this ILogger<AuthorizationController> logger, string? grantType);

    [LoggerMessage(LogLevel.Debug, "Processing client_credentials grant for ClientId: {ClientId}")]
    internal static partial void ProcessingClientCredentialsGrant(this ILogger<AuthorizationController> logger, string clientId);

    [LoggerMessage(LogLevel.Warning, "Rejecting token request: client credentials are missing")]
    internal static partial void MissingClientCredentials(this ILogger<AuthorizationController> logger);

    [LoggerMessage(LogLevel.Warning, "Rejecting token request: application not found for ClientId: {ClientId}")]
    internal static partial void ApplicationNotFound(this ILogger<AuthorizationController> logger, string clientId);

    [LoggerMessage(LogLevel.Debug, "Verifying client secret for ClientId: {ClientId}")]
    internal static partial void VerifyingClientSecret(this ILogger<AuthorizationController> logger, string clientId);

    [LoggerMessage(LogLevel.Warning, "Rejecting token request: secret verification failed for ClientId: {ClientId}")]
    internal static partial void SecretVerificationFailed(this ILogger<AuthorizationController> logger, string clientId);

    [LoggerMessage(LogLevel.Debug, "Client credentials validated successfully for ClientId: {ClientId}")]
    internal static partial void ClientCredentialsValidated(this ILogger<AuthorizationController> logger, string clientId);

    [LoggerMessage(LogLevel.Debug, "Processing password grant for user: {Username}")]
    internal static partial void ProcessingPasswordGrant(this ILogger<AuthorizationController> logger, string username);

    [LoggerMessage(LogLevel.Warning, "Password sign-in failed for user: {Username}")]
    internal static partial void PasswordSignInFailed(this ILogger<AuthorizationController> logger, string username);

    [LoggerMessage(LogLevel.Debug, "Password validated successfully for user: {Username}")]
    internal static partial void PasswordValidated(this ILogger<AuthorizationController> logger, string username);

    [LoggerMessage(LogLevel.Debug, "Processing refresh token grant")]
    internal static partial void ProcessingRefreshTokenGrant(this ILogger<AuthorizationController> logger);

    [LoggerMessage(LogLevel.Warning, "Refresh token grant failed: user not found")]
    internal static partial void RefreshTokenUserNotFound(this ILogger<AuthorizationController> logger);
}
