// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class OAuthTokenProviderLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Initializing OAuth token provider for endpoint: {Endpoint}")]
    internal static partial void InitializingTokenProvider(this ILogger<OAuthTokenProvider> logger, string endpoint);

    [LoggerMessage(LogLevel.Debug, "Using cached access token (expires at {ExpiryTime})")]
    internal static partial void UsingCachedToken(this ILogger<OAuthTokenProvider> logger, DateTimeOffset expiryTime);

    [LoggerMessage(LogLevel.Debug, "Requesting new access token from {Endpoint}")]
    internal static partial void RequestingAccessToken(this ILogger<OAuthTokenProvider> logger, string endpoint);

    [LoggerMessage(LogLevel.Debug, "Successfully obtained access token, expires in {ExpiresIn} seconds")]
    internal static partial void ObtainedAccessToken(this ILogger<OAuthTokenProvider> logger, int expiresIn);

    [LoggerMessage(LogLevel.Error, "Token request failed with status {StatusCode} ({ReasonPhrase}): {ErrorContent}")]
    internal static partial void TokenRequestFailed(this ILogger<OAuthTokenProvider> logger, int statusCode, string reasonPhrase, string errorContent);

    [LoggerMessage(LogLevel.Error, "Failed to obtain access token from {Endpoint}")]
    internal static partial void FailedToObtainAccessToken(this ILogger<OAuthTokenProvider> logger, string endpoint, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Refreshing access token by clearing cache and requesting new token")]
    internal static partial void RefreshingAccessToken(this ILogger<OAuthTokenProvider> logger);

    [LoggerMessage(LogLevel.Warning, "Accepting self-signed certificate for {CertificateSubject} (development mode)")]
    internal static partial void AcceptingSelfSignedCertificate(this ILogger<OAuthTokenProvider> logger, string certificateSubject);

    [LoggerMessage(LogLevel.Error, "Certificate validation failed: {SslPolicyErrors}")]
    internal static partial void CertificateValidationFailed(this ILogger<OAuthTokenProvider> logger, string sslPolicyErrors);
}
