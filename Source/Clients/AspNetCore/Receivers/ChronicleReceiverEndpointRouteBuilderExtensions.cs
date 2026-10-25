// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Receivers;
using Cratis.Chronicle.Receivers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Maps the unavailable Chronicle receiver to a fail-closed endpoint.
/// </summary>
internal static class ChronicleReceiverEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a POST receiver at the provisional route <c language="csharp">{path}/{eventStore}/{namespace}/{reactor|reducer}/{observerId}</c>.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="path">The base path, defaulting to <c language="json">Serverless:ReceiverPath</c>.</param>
    /// <returns>The endpoint convention builder.</returns>
    /// <remarks>
    /// Every request returns 503 without reading its body. HTTP delivery is unavailable until the kernel
    /// envelope and signature verification ship. Host authorization cannot replace signature verification.
    /// </remarks>
    internal static IEndpointConventionBuilder MapChronicleReceiver(this IEndpointRouteBuilder endpoints, string? path = null)
    {
        path ??= endpoints.ServiceProvider.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value.Serverless.ReceiverPath;
        return endpoints.MapPost(ChronicleReceiverRoute.Pattern(path), RefuseUnsigned).WithDisplayName("Chronicle receiver");
    }

    /// <summary>
    /// Provides an internal dispatch seam for specs with already verified deliveries.
    /// No production endpoint maps this handler until signature verification is available.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The completion task.</returns>
    internal static async Task HandleVerifiedDelivery(HttpContext context)
    {
        var services = context.RequestServices;
        var resolver = services.GetRequiredService<IDeliveryTargetResolver>();
        var receiver = services.GetRequiredService<IChronicleReceiver>();
        var statusCodes = services.GetRequiredService<IDeliveryStatusCodes>();
        var outcome = await receiver.Receive(new(context.Request.Body, resolver.Resolve(context)) { SignatureVerified = true }, context.RequestAborted);
        context.Response.StatusCode = statusCodes.For(outcome);
    }

    static Task RefuseUnsigned(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return Task.CompletedTask;
    }
}
