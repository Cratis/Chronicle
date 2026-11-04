// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class OAuthTokenProviderLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Using cached access token (expires at {ExpiryTime})")]
    internal static partial void UsingCachedToken(this ILogger<OAuthTokenProvider> logger, DateTimeOffset expiryTime);

    [LoggerMessage(LogLevel.Debug, "Requesting new access token from the {EndpointSource} token endpoint")]
    internal static partial void RequestingAccessToken(this ILogger<OAuthTokenProvider> logger, string endpointSource);

    [LoggerMessage(LogLevel.Debug, "Successfully obtained access token, expires in {ExpiresIn} seconds")]
    internal static partial void ObtainedAccessToken(this ILogger<OAuthTokenProvider> logger, int expiresIn);

    [LoggerMessage(LogLevel.Warning, "Token request to the {EndpointSource} token endpoint failed with status {StatusCode} ({StatusName}), OAuth error {ErrorCode}")]
    internal static partial void TokenRequestFailed(this ILogger<OAuthTokenProvider> logger, string endpointSource, int statusCode, string statusName, string errorCode);

    [LoggerMessage(LogLevel.Error, "Token request to the {EndpointSource} token endpoint was refused with status {StatusCode} ({StatusName}), OAuth error {ErrorCode}")]
    internal static partial void TokenRequestRejected(this ILogger<OAuthTokenProvider> logger, string endpointSource, int statusCode, string statusName, string errorCode);

    [LoggerMessage(LogLevel.Debug, "Could not use the {EndpointSource} token endpoint (reason: {Reason}); the next connect attempt retries")]
    internal static partial void TokenEndpointUnreachable(this ILogger<OAuthTokenProvider> logger, string endpointSource, string reason);

    [LoggerMessage(LogLevel.Error, "Failed to obtain access token from the {EndpointSource} token endpoint (reason: {Reason})")]
    internal static partial void FailedToObtainAccessToken(this ILogger<OAuthTokenProvider> logger, string endpointSource, string reason);

    [LoggerMessage(LogLevel.Debug, "Refreshing access token by clearing cache and requesting new token")]
    internal static partial void RefreshingAccessToken(this ILogger<OAuthTokenProvider> logger);

    [LoggerMessage(LogLevel.Debug, "Skipping token fetch - the previous attempt failed moments ago")]
    internal static partial void ThrottlingTokenFetch(this ILogger<OAuthTokenProvider> logger);

    [LoggerMessage(LogLevel.Warning, "Failed to fetch new access token from the {EndpointSource} token endpoint (reason: {Reason}) - serving cached token valid until {ExpiryTime}")]
    internal static partial void ServingCachedTokenAfterFailedFetch(this ILogger<OAuthTokenProvider> logger, string endpointSource, DateTimeOffset expiryTime, string reason);
}
