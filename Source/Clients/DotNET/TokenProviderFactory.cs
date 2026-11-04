// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Selects the <see cref="ITokenProvider"/> for a client based on its options.
/// </summary>
static class TokenProviderFactory
{
    /// <summary>
    /// Creates the <see cref="ITokenProvider"/> for the given options.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/>.</param>
    /// <param name="serverAddress">Provides the address of the Chronicle server currently in use.</param>
    /// <param name="skipTlsValidation">Whether to skip TLS validation towards the Chronicle server. Never applies to external authorities.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <returns>The <see cref="ITokenProvider"/>.</returns>
    /// <exception cref="ClientCredentialsRequiredForExternalTokenAuthority">Thrown when an external authority is configured without client credentials.</exception>
    /// <exception cref="InsecureTokenEndpoint">Thrown when a configured endpoint is not an absolute https URL without user info.</exception>
    internal static ITokenProvider Create(
        ChronicleOptions options,
        Func<ChronicleServerAddress> serverAddress,
        bool skipTlsValidation,
        ILoggerFactory loggerFactory)
    {
        if (options.ConnectionString.AuthenticationMode != AuthenticationMode.ClientCredentials)
        {
            return new NoOpTokenProvider();
        }

        var username = options.ConnectionString.Username;
        var password = options.ConnectionString.Password;
        var sourceLogger = loggerFactory.CreateLogger("Cratis.Chronicle.TokenProviderFactory");
        var hasTokenEndpoint = !string.IsNullOrWhiteSpace(options.Authentication.TokenEndpoint);
        var hasAuthority = !string.IsNullOrWhiteSpace(options.Authentication.Authority);

        if (!hasTokenEndpoint && !hasAuthority)
        {
            if (string.IsNullOrEmpty(username) &&
                string.IsNullOrEmpty(password))
            {
                username = ChronicleConnectionString.DevelopmentClient;
                password = ChronicleConnectionString.DevelopmentClientSecret;
            }

            var defaultAddress = options.ConnectionString.ServerAddress;
            sourceLogger.UsingServerDefaultTokenEndpoint($"https://{defaultAddress.Host}:{defaultAddress.Port}/connect/token");
            return new OAuthTokenProvider(
                serverAddress,
                username!,
                password!,
                skipTlsValidation,
                loggerFactory.CreateLogger<OAuthTokenProvider>());
        }

        if (LacksExplicitCredentials(options.ConnectionString))
        {
            throw new ClientCredentialsRequiredForExternalTokenAuthority();
        }

        var scope = options.Authentication.Scope;
        if (hasTokenEndpoint)
        {
            var tokenEndpoint = ParseOrThrow(options.Authentication.TokenEndpoint!);
            sourceLogger.UsingConfiguredTokenEndpoint(ExternalTokenEndpoints.Describe(tokenEndpoint));
#pragma warning disable CA2000 // The handler is owned and disposed by the token provider
            return new OAuthTokenProvider(
                tokenEndpoint,
                username!,
                password!,
                scope,
                loggerFactory.CreateLogger<OAuthTokenProvider>(),
                OAuthTokenProvider.CreateExternalHttpMessageHandler(),
                TimeProvider.System);
#pragma warning restore CA2000
        }

        var authority = ParseOrThrow(options.Authentication.Authority!);
        sourceLogger.UsingAuthorityDiscovery(ExternalTokenEndpoints.Describe(authority));
        return new ExternalAuthorityTokenProvider(
            authority,
            username!,
            password!,
            scope,
            loggerFactory.CreateLogger<ExternalAuthorityTokenProvider>(),
            loggerFactory.CreateLogger<OAuthTokenProvider>());
    }

    /// <summary>
    /// Gets whether a connection string carries no client credentials of its own - none at all, or only the well-known development ones.
    /// </summary>
    /// <param name="connectionString">The <see cref="ChronicleConnectionString"/> to check.</param>
    /// <returns>True if there are no explicit credentials, false otherwise.</returns>
    internal static bool LacksExplicitCredentials(ChronicleConnectionString connectionString) =>
        (string.IsNullOrEmpty(connectionString.Username) && string.IsNullOrEmpty(connectionString.Password)) ||
        (connectionString.Username == ChronicleConnectionString.DevelopmentClient &&
         connectionString.Password == ChronicleConnectionString.DevelopmentClientSecret);

    static Uri ParseOrThrow(string value) =>
        ExternalTokenEndpoints.Require(Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : new Uri("invalid:///", UriKind.Absolute));
}
