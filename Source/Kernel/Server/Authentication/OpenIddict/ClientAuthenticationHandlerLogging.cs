// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

#if DEVELOPMENT

/// <summary>
/// Logging extensions for <see cref="ClientAuthenticationHandler"/>.
/// </summary>
internal static partial class ClientAuthenticationHandlerLogging
{
    [LoggerMessage(LogLevel.Debug, "Handling token request for client '{ClientId}'")]
    internal static partial void HandlingTokenRequest(this ILogger<ClientAuthenticationHandler> logger, string? clientId);

    [LoggerMessage(LogLevel.Warning, "Application not found for client '{ClientId}'")]
    internal static partial void ApplicationNotFound(this ILogger<ClientAuthenticationHandler> logger, string? clientId);

    [LoggerMessage(LogLevel.Debug, "Application found for client '{ClientId}'")]
    internal static partial void ApplicationFound(this ILogger<ClientAuthenticationHandler> logger, string? clientId);

    [LoggerMessage(LogLevel.Warning, "Secret verification failed for client '{ClientId}'")]
    internal static partial void SecretVerificationFailed(this ILogger<ClientAuthenticationHandler> logger, string? clientId);

    [LoggerMessage(LogLevel.Debug, "Secret verification succeeded for client '{ClientId}'")]
    internal static partial void SecretVerificationSucceeded(this ILogger<ClientAuthenticationHandler> logger, string? clientId);
}

#endif
