// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents the caller of a call that uses a client connection.
/// </summary>
/// <param name="Owner">The <see cref="ConnectionOwner"/> identity of the caller.</param>
/// <param name="Principal">The <see cref="ClaimsPrincipal"/> of the caller, if there is one.</param>
/// <param name="RequestServices">The request <see cref="IServiceProvider"/>, if there is one.</param>
public record ConnectionCaller(ConnectionOwner Owner, ClaimsPrincipal? Principal, IServiceProvider? RequestServices)
{
    /// <summary>
    /// Gets the caller for calls made inside the server process.
    /// </summary>
    public static ConnectionCaller InProcess { get; } = new(ConnectionOwner.InProcess, null, null);

    /// <summary>
    /// Gets the caller used when authentication is disabled.
    /// </summary>
    public static ConnectionCaller Anonymous { get; } = new(ConnectionOwner.Anonymous, null, null);

    /// <summary>
    /// Creates the caller of a request.
    /// </summary>
    /// <param name="http">The <see cref="HttpContext"/> of the request, if it is available.</param>
    /// <param name="authenticationEnabled">Whether authentication is enabled.</param>
    /// <returns>The <see cref="ConnectionCaller"/>.</returns>
    public static ConnectionCaller For(HttpContext? http, bool authenticationEnabled)
    {
        if (!authenticationEnabled) return new(ConnectionOwner.Anonymous, http?.User, http?.RequestServices);
        if (http is null) return new(ConnectionOwner.Unidentified, null, null);
        return new(ConnectionOwner.From(http.User), http.User, http.RequestServices);
    }
}
