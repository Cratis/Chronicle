// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Refuses non-health requests with retryable responses until the kernel is ready.
/// </summary>
/// <param name="next">The next request handler.</param>
/// <param name="state">The kernel startup state.</param>
/// <param name="options">The Chronicle configuration.</param>
internal sealed class KernelStartupGate(RequestDelegate next, KernelStartupState state, IOptions<ChronicleOptions> options)
{
    /// <summary>
    /// Passes ready or health requests through and refuses other requests during startup or shutdown.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <returns>A task representing request handling.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        if (state.IsReady || HealthOnlyPortPolicy.IsHealthCheckEndpoint(options.Value.HealthCheckEndpoint, context.Request.Path.Value))
        {
            return next(context);
        }

        if (context.Request.ContentType?.StartsWith("application/grpc", StringComparison.OrdinalIgnoreCase) == true)
        {
            // A trailers-only gRPC response carries its status in the initial HEADERS frame, with END_STREAM.
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/grpc";
            context.Response.Headers.GrpcStatus = "14";
            context.Response.Headers.GrpcMessage = "Chronicle is not ready.";
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "1";
        }

        return Task.CompletedTask;
    }
}
