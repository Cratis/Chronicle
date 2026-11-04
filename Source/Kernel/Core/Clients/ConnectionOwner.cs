// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Identifies the principal that opened a connection.
/// </summary>
/// <param name="Value">The stable principal key.</param>
public record ConnectionOwner(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the owner used when authentication is disabled.
    /// </summary>
    public static readonly ConnectionOwner Anonymous = new("anonymous");

    /// <summary>
    /// Gets the owner for a network caller without a usable identity.
    /// </summary>
    public static readonly ConnectionOwner Unidentified = new("unidentified");

    /// <summary>
    /// Gets the owner for direct in-process calls.
    /// </summary>
    public static readonly ConnectionOwner InProcess = new("in-process");

    /// <summary>
    /// Creates a stable key from an authenticated principal.
    /// </summary>
    /// <param name="principal">The calling principal.</param>
    /// <returns>The principal key, or <see cref="Unidentified"/>.</returns>
    public static ConnectionOwner From(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated is not true) return Unidentified;
        var subject = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subject)) return Unidentified;
        var kind = principal.HasClaim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User) ||
            principal.HasClaim(ChronicleClaims.PrincipalKind, ChronicleClaims.User) ? "user" : "client";
        var issuer = principal.FindFirst("iss")?.Value ?? string.Empty;

        return new($"{kind}|{issuer.Length}:{issuer}{subject.Length}:{subject}");
    }
}
