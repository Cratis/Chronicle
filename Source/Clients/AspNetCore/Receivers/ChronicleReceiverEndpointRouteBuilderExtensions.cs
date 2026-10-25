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
/// Maps the preview Chronicle delivery receiver into an ASP.NET Core host.
/// </summary>
public static class ChronicleReceiverEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a POST receiver at the provisional route <c language="csharp">{path}/{eventStore}/{namespace}/{reactor|reducer}/{observerId}</c>.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="path">The base path, defaulting to <c language="json">Serverless:ReceiverPath</c>.</param>
    /// <returns>The endpoint convention builder, allowing host authorization conventions.</returns>
    /// <remarks>
    /// The endpoint is not gated on serverless mode. The host owns authentication: use
    /// <c language="csharp">RequireAuthorization()</c> or protect the endpoint at the hosting layer.
    /// This preview verifies no delivery signature. The route and status codes are provisional until the HTTP
    /// delivery contract is settled. The server's request body limit applies before the receiver's local limit.
    /// </remarks>
    public static IEndpointConventionBuilder MapChronicleReceiver(this IEndpointRouteBuilder endpoints, string? path = null)
    {
        path ??= endpoints.ServiceProvider.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value.Serverless.ReceiverPath;
        return endpoints.MapPost(ChronicleReceiverRoute.Pattern(path), Handle).WithDisplayName("Chronicle receiver");
    }

    /// <summary>
    /// Adapts an HTTP request and outcome without interpreting the body.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The completion task.</returns>
    static async Task Handle(HttpContext context)
    {
        var services = context.RequestServices;
        var resolver = services.GetRequiredService<IDeliveryTargetResolver>();
        var receiver = services.GetRequiredService<IChronicleReceiver>();
        var statusCodes = services.GetRequiredService<IDeliveryStatusCodes>();
        var outcome = await receiver.Receive(new(context.Request.Body, resolver.Resolve(context)), context.RequestAborted);
        context.Response.StatusCode = statusCodes.For(outcome);
    }
}
