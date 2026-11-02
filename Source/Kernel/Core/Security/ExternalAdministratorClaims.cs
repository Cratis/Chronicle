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
        if (mapping is null || string.IsNullOrWhiteSpace(mapping.Type) || string.IsNullOrWhiteSpace(mapping.Value)) return false;

        foreach (var identity in principal.Identities)
        {
            if (!identity.IsAuthenticated || !string.Equals(identity.AuthenticationType, ExternalAuthority.AuthenticationType, StringComparison.Ordinal)) continue;
            foreach (var claim in identity.Claims)
            {
                if (string.Equals(claim.Value, mapping.Value, StringComparison.Ordinal) && HasType(claim, mapping.Type)) return true;
            }
        }

        return false;
    }

    static bool HasType(Claim claim, string type) =>
        string.Equals(claim.Type, type, StringComparison.Ordinal) ||
        (claim.Properties.TryGetValue(ExternalAuthority.ShortClaimTypeProperty, out var shortType) && string.Equals(shortType, type, StringComparison.Ordinal));
}
