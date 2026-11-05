// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents the identity of the caller that opened a client connection.
/// </summary>
/// <param name="Issuer">The issuer of the subject claim.</param>
/// <param name="Subject">The subject of the caller, or the client identifier when there is no subject. Empty for the well-known owners.</param>
/// <param name="IsClient">Whether <paramref name="Subject"/> is a client identifier rather than a subject.</param>
/// <remarks>
/// All parts are compared with ordinal equality. The well-known owners have an empty subject, so they can
/// never equal an owner derived from a real principal, and a client never equals a subject with the same value.
/// </remarks>
public readonly record struct ConnectionOwner(string Issuer, string Subject, bool IsClient = false)
{
    static readonly string[] _subjectClaimTypes = ["sub", ClaimTypes.NameIdentifier];
    static readonly string[] _clientClaimTypes = ["azp", "client_id"];

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
    /// <returns>The <see cref="ConnectionOwner"/>, or <see cref="Unidentified"/> when the principal is not authenticated or has neither a subject nor a client identifier.</returns>
    public static ConnectionOwner From(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated is not true) return Unidentified;

        var subject = FirstNonEmpty(principal, _subjectClaimTypes);
        if (subject is not null) return new(subject.Issuer, subject.Value);

        var client = FirstNonEmpty(principal, _clientClaimTypes);
        return client is null ? Unidentified : new(client.Issuer, client.Value, true);
    }

    static Claim? FirstNonEmpty(ClaimsPrincipal principal, string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            foreach (var claim in principal.FindAll(claimType))
            {
                if (!string.IsNullOrEmpty(claim.Value)) return claim;
            }
        }

        return null;
    }
}
