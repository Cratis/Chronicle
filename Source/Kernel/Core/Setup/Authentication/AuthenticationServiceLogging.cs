// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup.Authentication;

#pragma warning disable MA0182 // Unused internal type. This is used during DEVELOPMENT builds only.
internal static partial class AuthenticationServiceLogging
{
    [LoggerMessage(LogLevel.Debug, "Checking for existing default admin user")]
    internal static partial void CheckingForDefaultAdminUser(this ILogger<AuthenticationService> logger);

    [LoggerMessage(LogLevel.Debug, "Default admin user already exists")]
    internal static partial void DefaultAdminUserAlreadyExist(this ILogger<AuthenticationService> logger);

    [LoggerMessage(LogLevel.Information, "Creating default admin user")]
    internal static partial void CreatingDefaultAdminUser(this ILogger<AuthenticationService> logger);

    [LoggerMessage(LogLevel.Information, "Successfully created default admin user")]
    internal static partial void DefaultAdminUserAdded(this ILogger<AuthenticationService> logger);

    [LoggerMessage(LogLevel.Information, "Setting pre-configured default admin password from development configuration")]
    internal static partial void SettingDefaultAdminPassword(this ILogger<AuthenticationService> logger);

    [LoggerMessage(LogLevel.Information, "Default admin password pre-configured successfully")]
    internal static partial void DefaultAdminPasswordSet(this ILogger<AuthenticationService> logger);

    [LoggerMessage(LogLevel.Information, "Bootstrapping {Count} client(s) from configuration")]
    internal static partial void BootstrappingClients(this ILogger<AuthenticationService> logger, int count);

    [LoggerMessage(LogLevel.Warning, "Skipping bootstrap client with invalid configuration: ClientId={ClientId}")]
    internal static partial void SkippingInvalidBootstrapClient(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Debug, "Bootstrap client already exists: {ClientId}")]
    internal static partial void BootstrapClientAlreadyExists(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Information, "Registering bootstrap client: {ClientId}")]
    internal static partial void RegisteringBootstrapClient(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Information, "Successfully registered bootstrap client: {ClientId}")]
    internal static partial void BootstrapClientRegistered(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Debug, "Checking for existing default client credentials with ClientId: {ClientId}")]
    internal static partial void CheckingForDefaultClientCredentials(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Debug, "Default client credentials already exist for ClientId: {ClientId}")]
    internal static partial void DefaultClientCredentialsAlreadyExist(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Information, "Creating default client credentials for development with ClientId: {ClientId}")]
    internal static partial void CreatingDefaultClientCredentials(this ILogger<AuthenticationService> logger, string clientId);

    [LoggerMessage(LogLevel.Information, "Successfully created default client credentials with ClientId: {ClientId}")]
    internal static partial void DefaultClientCredentialsCreated(this ILogger<AuthenticationService> logger, string clientId);
}
