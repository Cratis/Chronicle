// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

internal static partial class GrpcAuthenticationMiddlewareLogging
{
    [LoggerMessage(LogLevel.Debug, "gRPC request detected")]
    internal static partial void GrpcRequestDetected(this ILogger<GrpcAuthenticationMiddleware> logger);

    [LoggerMessage(LogLevel.Debug, "Authorization header present in gRPC request")]
    internal static partial void AuthorizationHeaderFound(this ILogger<GrpcAuthenticationMiddleware> logger);

    [LoggerMessage(LogLevel.Debug, "No Authorization header found in gRPC request")]
    internal static partial void NoAuthorizationHeaderFound(this ILogger<GrpcAuthenticationMiddleware> logger);
}
