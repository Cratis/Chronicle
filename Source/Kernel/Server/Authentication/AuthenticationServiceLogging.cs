// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

internal static partial class AuthenticationServiceLogging
{
    [LoggerMessage(LogLevel.Information, "Checking for existing default client credentials with ClientId: {ClientId}")]
    internal static partial void CheckingForDefaultClientCredentials(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Information, "Default client credentials already exist for ClientId: {ClientId}")]
    internal static partial void DefaultClientCredentialsAlreadyExist(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Information, "Creating default client credentials for development with ClientId: {ClientId}")]
    internal static partial void CreatingDefaultClientCredentials(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Information, "Successfully created default client credentials with ClientId: {ClientId}")]
    internal static partial void DefaultClientCredentialsCreated(this ILogger<AuthenticationService> logger, string clientId);
}
