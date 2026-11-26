// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Identifies users a trusted reverse proxy authenticated, and decides what they are allowed.
/// </summary>
public static class TrustedProxyUsers
{
    /// <summary>
    /// The authentication type set on identities built from trusted proxy headers.
    /// </summary>
    public const string AuthenticationType = "Chronicle.TrustedProxy";

    /// <summary>
    /// The claim type carrying each group the proxy forwarded.
    /// </summary>
    public const string GroupClaimType = "groups";

    /// <summary>
    /// Checks whether the principal was authenticated by a trusted reverse proxy.
    /// </summary>
    /// <param name="principal">The principal to check.</param>
    /// <returns>True if one of its identities came from trusted proxy headers.</returns>
    public static bool Is(ClaimsPrincipal? principal) =>
        principal?.Identities.Any(IsProxyIdentity) is true;

    /// <summary>
    /// Checks whether a proxy-authenticated principal is a Chronicle administrator.
    /// </summary>
    /// <param name="principal">The principal to check.</param>
    /// <param name="authentication">The authentication configuration.</param>
    /// <returns>True if trusted proxy authentication is on and the principal holds the administrator group, or no group is configured.</returns>
    public static bool IsAdministrator(ClaimsPrincipal principal, Authentication authentication)
    {
        if (!authentication.Http.IsTrustedProxy) return false;
        var identity = principal.Identities.FirstOrDefault(IsProxyIdentity);
        if (identity is null) return false;
        var group = authentication.Http.TrustedProxy.AdministratorGroup;
        if (string.IsNullOrWhiteSpace(group)) return true;
        return identity.Claims.Any(_ => _.Type == GroupClaimType && string.Equals(_.Value, group, StringComparison.Ordinal));
    }

    static bool IsProxyIdentity(ClaimsIdentity identity) =>
        identity.IsAuthenticated && string.Equals(identity.AuthenticationType, AuthenticationType, StringComparison.Ordinal);
}
