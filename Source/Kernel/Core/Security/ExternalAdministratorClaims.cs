// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Matches the configured external administrator claim against external-authority identities.
/// </summary>
public static class ExternalAdministratorClaims
{
    /// <summary>
    /// Gets whether the principal carries the mapped claim on an authenticated external-authority identity.
    /// </summary>
    /// <param name="principal">The principal to inspect.</param>
    /// <param name="mapping">The configured mapping, if any.</param>
    /// <returns>True when granted; otherwise false. Matching is ordinal and case-sensitive on the whole value.</returns>
    public static bool Grants(ClaimsPrincipal principal, ExternalAdministratorClaim? mapping)
    {
        if (!IsComplete(mapping)) return false;

        foreach (var identity in principal.Identities)
        {
            if (IsExternalAuthority(identity) && HasMappedClaim(identity, mapping!.Type, mapping.Value)) return true;
        }

        return false;
    }

    static bool IsComplete(ExternalAdministratorClaim? mapping) =>
        mapping is not null && !string.IsNullOrWhiteSpace(mapping.Type) && !string.IsNullOrWhiteSpace(mapping.Value);

    static bool IsExternalAuthority(ClaimsIdentity identity) =>
        identity.IsAuthenticated && string.Equals(identity.AuthenticationType, ExternalAuthority.AuthenticationType, StringComparison.Ordinal);

    static bool HasMappedClaim(ClaimsIdentity identity, string? type, string? value)
    {
        foreach (var claim in identity.Claims)
        {
            if (string.Equals(claim.Value, value, StringComparison.Ordinal) && HasType(claim, type)) return true;
        }

        return false;
    }

    static bool HasType(Claim claim, string? type) =>
        string.Equals(claim.Type, type, StringComparison.Ordinal) ||
        (claim.Properties.TryGetValue(ExternalAuthority.ShortClaimTypeProperty, out var shortType) && string.Equals(shortType, type, StringComparison.Ordinal));
}
