// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Describes the HTTP request that caused an append, so the event can say where it came from.
/// </summary>
/// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> for the request in flight.</param>
/// <remarks>
/// Appends made from the Workbench are operator actions, and an operator action is only auditable if the request
/// behind it travels with the event. The chain is built from the live request rather than accumulated up front,
/// because these commands each map to exactly one request.
/// </remarks>
public class RequestCausation(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>
    /// The causation type recorded for an HTTP request.
    /// </summary>
    public const string CausationType = "ASP.NET Request";

    const string RouteProperty = "route";
    const string MethodProperty = "method";
    const string HostProperty = "host";
    const string ProtocolProperty = "protocol";
    const string SchemeProperty = "scheme";
    const string QueryProperty = "query";
    const string OriginProperty = "origin";
    const string RefererProperty = "referer";
    const string RouteValuePrefix = "route-value";

    /// <summary>
    /// Gets the causation chain for the request in flight.
    /// </summary>
    /// <returns>A chain with one entry describing the request, or empty when there is no request.</returns>
    public IList<Contracts.Auditing.Causation> GetCurrentChain()
    {
        if (httpContextAccessor.HttpContext is not { } context)
        {
            return [];
        }

        var request = context.Request;
        var properties = new Dictionary<string, string>
        {
            { RouteProperty, request.Path },
            { MethodProperty, request.Method },
            { HostProperty, request.Host.Value ?? string.Empty },
            { ProtocolProperty, request.Protocol },
            { SchemeProperty, request.Scheme },
            { QueryProperty, request.QueryString.ToString() }
        };

        if (request.Headers.Origin != StringValues.Empty)
        {
            properties[OriginProperty] = request.Headers.Origin.ToString();
        }

        if (request.Headers.Referer != StringValues.Empty)
        {
            properties[RefererProperty] = request.Headers.Referer.ToString();
        }

        request.RouteValues.ForEach(routeValue => properties.Add($"{RouteValuePrefix}:{routeValue.Key}", routeValue.Value?.ToString() ?? string.Empty));

        return
        [
            new Contracts.Auditing.Causation
            {
                Occurred = DateTimeOffset.UtcNow,
                Type = CausationType,
                Properties = properties
            }
        ];
    }
}
