// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Applies the anonymous-access policy to the small subset of ASP.NET Identity endpoints Chronicle exposes publicly.
/// </summary>
internal static class IdentityEndpointAuthorization
{
    /// <summary>
    /// Applies endpoint-specific anonymous metadata. The returned MapIdentityApi group must not be marked anonymous.
    /// </summary>
    /// <param name="endpoints">The mapped Identity endpoints.</param>
    public static void Apply(IEndpointConventionBuilder endpoints) => endpoints.Add(Apply);

    /// <summary>
    /// Applies anonymous metadata to an individual Identity endpoint when it is explicitly allowed.
    /// </summary>
    /// <param name="endpoint">The endpoint being configured.</param>
    internal static void Apply(EndpointBuilder endpoint)
    {
        if (endpoint is RouteEndpointBuilder route && IsAnonymousRoute(route.RoutePattern.RawText))
        {
            endpoint.Metadata.Add(new AllowAnonymousAttribute());
            endpoint.Metadata.Add(new IgnoreAntiforgeryTokenAttribute());
        }
    }

    /// <summary>
    /// Gets whether an Identity route is part of Chronicle's anonymous login/token-refresh surface.
    /// </summary>
    /// <param name="route">The route pattern.</param>
    /// <returns>True when anonymous access is allowed.</returns>
    internal static bool IsAnonymousRoute(string? route) =>
        route?.EndsWith("/login", StringComparison.Ordinal) is true ||
        route?.EndsWith("/refresh", StringComparison.Ordinal) is true;
}
