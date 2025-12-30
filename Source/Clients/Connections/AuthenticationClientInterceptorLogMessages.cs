// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class AuthenticationClientInterceptorLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Adding authentication header to gRPC call: {Method}")]
    internal static partial void AddingAuthenticationHeader(this ILogger<AuthenticationClientInterceptor> logger, string method);

    [LoggerMessage(LogLevel.Trace, "No authentication token available, skipping authentication header for: {Method}")]
    internal static partial void NoTokenAvailable(this ILogger<AuthenticationClientInterceptor> logger, string method);

    [LoggerMessage(LogLevel.Warning, "Failed to obtain authentication token for gRPC call: {Method}")]
    internal static partial void FailedToObtainToken(this ILogger<AuthenticationClientInterceptor> logger, string method, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Authentication failed (Unauthenticated) for {Method}, retrying with token refresh")]
    internal static partial void AuthenticationFailedRetryingWithTokenRefresh(this ILogger<AuthenticationClientInterceptor> logger, string method);

    [LoggerMessage(LogLevel.Information, "Retrying gRPC call {Method} after token refresh")]
    internal static partial void RetryingCallAfterTokenRefresh(this ILogger<AuthenticationClientInterceptor> logger, string method);

    [LoggerMessage(LogLevel.Error, "Retry failed after token refresh for {Method}")]
    internal static partial void RetryAfterTokenRefreshFailed(this ILogger<AuthenticationClientInterceptor> logger, string method, Exception ex);
}
