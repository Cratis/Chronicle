// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Middleware to ensure gRPC metadata authorization headers are visible to authentication middleware.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class GrpcAuthenticationMiddleware(RequestDelegate next, ILogger<GrpcAuthenticationMiddleware> logger)
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>Awaitable task.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is a gRPC request (content-type contains grpc)
        var contentType = context.Request.ContentType;
        if (contentType?.Contains("grpc") == true)
        {
            logger.GrpcRequestDetected();
            foreach (var header in context.Request.Headers)
            {
                logger.Header(header.Key, header.Value!);
            }

            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                logger.AuthorizationHeaderFound(authHeader!);
            }
            else
            {
                logger.NoAuthorizationHeaderFound();
            }
        }

        await next(context);
    }
}
