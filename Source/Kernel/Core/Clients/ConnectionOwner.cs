// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents the identity of the caller that opened a client connection.
/// </summary>
/// <param name="Issuer">The issuer of the subject claim.</param>
/// <param name="Subject">The subject of the caller. Empty for the well-known owners.</param>
/// <remarks>
/// Both parts are compared with ordinal string equality. The well-known owners have an empty subject, so they can
/// never equal an owner derived from a real principal.
/// </remarks>
public readonly record struct ConnectionOwner(string Issuer, string Subject)
{
    /// <summary>
    /// Gets the owner of every connection while authentication is disabled.
    /// </summary>
    public static ConnectionOwner Anonymous { get; } = new("chronicle:anonymous", string.Empty);

    /// <summary>
    /// Gets the owner of connections opened by callers inside the server process.
    /// </summary>
    public static ConnectionOwner InProcess { get; } = new("chronicle:in-process", string.Empty);

    /// <summary>
    /// Gets the owner used when the caller cannot be identified. It never owns a connection.
    /// </summary>
    public static ConnectionOwner Unidentified { get; } = new("chronicle:unidentified", string.Empty);

    /// <summary>
    /// Gets whether this is the owner used when the caller cannot be identified.
    /// </summary>
    public bool IsUnidentified => Equals(Unidentified);

    /// <summary>
    /// Derives the owner from a principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/>, if any.</param>
    /// <returns>The <see cref="ConnectionOwner"/>, or <see cref="Unidentified"/> when the principal is not authenticated or has no subject.</returns>
    public static ConnectionOwner From(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated is not true) return Unidentified;

        var claim = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null || string.IsNullOrEmpty(claim.Value)) return Unidentified;

        return new(claim.Issuer, claim.Value);
    }
}
