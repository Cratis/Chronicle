// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the issuer-qualified identity of a principal.
/// </summary>
/// <param name="Issuer">The normalized issuer. Principals of Chronicle's own authority share <see cref="LocalIssuer"/>.</param>
/// <param name="Subject">The subject, or the client identifier when there is no subject. Empty when unidentified.</param>
/// <param name="IsClient">Whether <paramref name="Subject"/> is a client identifier rather than a subject.</param>
/// <remarks>
/// A subject is only unique within its issuer. Two principals with the same subject but different issuers
/// have different keys.
/// </remarks>
[GenerateSerializer]
public readonly record struct PrincipalKey([property: Id(0)] string Issuer, [property: Id(1)] string Subject, [property: Id(2)] bool IsClient = false)
{
    /// <summary>
    /// The issuer shared by every principal of Chronicle's own authority.
    /// </summary>
    public const string LocalIssuer = "chronicle:local";

    /// <summary>
    /// Gets the key used when the caller cannot be identified.
    /// </summary>
    public static PrincipalKey Unidentified { get; } = new(ConnectionOwner.Unidentified.Issuer, string.Empty);

    /// <summary>
    /// Gets whether the caller could not be identified.
    /// </summary>
    public bool IsUnidentified => Equals(Unidentified);

    /// <summary>
    /// Derives the key from a principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/>, if any.</param>
    /// <param name="authentication">The <see cref="Authentication"/> configuration.</param>
    /// <returns>The <see cref="PrincipalKey"/>, or <see cref="Unidentified"/> when the principal cannot be identified.</returns>
    public static PrincipalKey From(ClaimsPrincipal? principal, Authentication authentication)
    {
        if (principal?.Identity?.IsAuthenticated is not true) return Unidentified;
        var subject = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        var claim = subject ?? principal.FindFirst("azp") ?? principal.FindFirst("client_id");
        if (claim is not null && !string.IsNullOrEmpty(claim.Value)) return new(Normalize(claim.Issuer, authentication), claim.Value, subject is null);
        var owner = ConnectionOwner.From(principal);
        return owner.IsUnidentified ? Unidentified : new(Normalize(owner.Issuer, authentication), owner.Subject, owner.IsClient);
    }

    /// <summary>
    /// Gets whether the principal is a signed-in user of Chronicle's own authority.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/>, if any.</param>
    /// <param name="authentication">The <see cref="Authentication"/> configuration.</param>
    /// <returns>True when a user-kind claim was issued by Chronicle's own authority; false otherwise.</returns>
    /// <remarks>
    /// A user-kind claim carried by a token of another authority never qualifies.
    /// </remarks>
    public static bool IsLocalUser(ClaimsPrincipal? principal, Authentication authentication) =>
        principal?.Identity?.IsAuthenticated is true &&
        principal.FindFirst(static claim => claim.Type == ChronicleClaimTypes.Principal || claim.Type == ChronicleClaims.PrincipalKind) is not null &&
        principal.Claims.Any(_ =>
            ((_.Type == ChronicleClaimTypes.Principal && _.Value == ChronicleClaimTypes.User) ||
             (_.Type == ChronicleClaims.PrincipalKind && _.Value == ChronicleClaims.User)) &&
            Normalize(_.Issuer, authentication) == LocalIssuer);

    /// <summary>
    /// Gets the actor recorded in audit entries for a principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/>, if any.</param>
    /// <param name="authentication">The <see cref="Authentication"/> configuration.</param>
    /// <returns>The key text, or "anonymous" when the caller cannot be identified.</returns>
    public static string Actor(ClaimsPrincipal? principal, Authentication authentication)
    {
        var key = From(principal, authentication);
        return key.IsUnidentified ? "anonymous" : key.ToString();
    }

    /// <inheritdoc/>
    public override string ToString() => IsClient ? $"{Issuer}|{Subject}|client" : $"{Issuer}|{Subject}";

    static string Normalize(string issuer, Authentication authentication) =>
        authentication.UseInternalAuthority || issuer == ClaimsIdentity.DefaultIssuer ? LocalIssuer : issuer;
}
