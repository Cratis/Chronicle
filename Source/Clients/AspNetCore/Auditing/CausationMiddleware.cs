// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.AspNetCore.Auditing;

/// <summary>
/// Represents a middleware for adding causation from ASP.NET requests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CausationMiddleware"/> class.
/// </remarks>
/// <param name="causationManager">The <see cref="ICausationManager"/> to use.</param>
/// <param name="next">The next middleware.</param>
public class CausationMiddleware(ICausationManager causationManager, RequestDelegate next)
{
    /// <summary>
    /// The causation property for the route.
    /// </summary>
    public const string CausationRouteProperty = "route";

    /// <summary>
    /// The causation property for the method.
    /// </summary>
    public const string CausationMethodProperty = "method";

    /// <summary>
    /// The causation property for the host.
    /// </summary>
    public const string CausationHostProperty = "host";

    /// <summary>
    /// The causation property for the protocol.
    /// </summary>
    public const string CausationProtocolProperty = "protocol";

    /// <summary>
    /// The causation property for the scheme.
    /// </summary>
    public const string CausationSchemeProperty = "scheme";

    /// <summary>
    /// The causation property for the query.
    /// </summary>
    public const string CausationQueryProperty = "query";

    /// <summary>
    /// The causation property for the HTTP Origin header value.
    /// </summary>
    public const string CausationOriginProperty = "origin";

    /// <summary>
    /// The causation property for the HTTP Referer value.
    /// </summary>
    public const string CausationRefererProperty = "referer";

    /// <summary>
    /// The causation property prefix for route values.
    /// </summary>
    public const string CausationRouteValuePrefix = "route-value";

    /// <summary>
    /// The causation type for ASP.NET requests.
    /// </summary>
    public static readonly CausationType CausationType = new("ASP.NET Request");

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>. </param>
    /// <returns>Awaitable task.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/.cratis"))
        {
            var properties = new Dictionary<string, string>
                {
                    { CausationRouteProperty, context.Request.Path },
                    { CausationMethodProperty, context.Request.Method },
                    { CausationHostProperty, context.Request.Host.Value ?? string.Empty },
                    { CausationProtocolProperty, context.Request.Protocol },
                    { CausationSchemeProperty, context.Request.Scheme },
                    { CausationQueryProperty, context.Request.QueryString.ToString() },
                };

            if (context.Request.Headers.Origin != StringValues.Empty)
            {
                properties[CausationOriginProperty] = context.Request.Headers.Origin.ToString();
            }

            if (context.Request.Headers.Origin != StringValues.Empty)
            {
                properties[CausationRefererProperty] = context.Request.Headers.Referer.ToString();
            }

            context.Request.RouteValues.ForEach(_ => properties.Add($"{CausationRouteValuePrefix}:{_.Key}", _.Value?.ToString() ?? string.Empty));
            causationManager.Add(CausationType, properties);
        }

        await next(context);
    }
}
