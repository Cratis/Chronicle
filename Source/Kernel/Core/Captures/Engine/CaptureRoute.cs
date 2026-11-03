// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Resolves capture routes within their external service's origin.
/// </summary>
public static class CaptureRoute
{
    /// <summary>
    /// Resolve a route against the external service's base address without leaving its origin.
    /// </summary>
    /// <param name="externalService">The name of the external service.</param>
    /// <param name="baseAddress">The external service's absolute base address.</param>
    /// <param name="route">The optional capture route.</param>
    /// <returns>The resolved URI on the external service's origin.</returns>
    /// <exception cref="CaptureRouteOutsideExternalService">The base address is missing or relative, or the route resolves to another origin.</exception>
    public static Uri Resolve(string externalService, Uri? baseAddress, string? route)
    {
        if (baseAddress?.IsAbsoluteUri != true)
        {
            throw new CaptureRouteOutsideExternalService(externalService);
        }

        var target = string.IsNullOrEmpty(route) ? baseAddress : new Uri(baseAddress, route);
        if (Uri.Compare(target, baseAddress, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
        {
            throw new CaptureRouteOutsideExternalService(externalService);
        }

        return target;
    }
}
