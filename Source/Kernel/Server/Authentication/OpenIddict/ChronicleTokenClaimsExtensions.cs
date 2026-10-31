// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using OpenIddict.Abstractions;
using User = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// Extension methods for applying Chronicle resource claims to token identities.
/// </summary>
internal static class ChronicleTokenClaimsExtensions
{
    /// <summary>
    /// Sets the Chronicle resource audience on an identity.
    /// </summary>
    /// <param name="identity">The identity to configure.</param>
    public static void SetChronicleAudience(this ClaimsIdentity identity) =>
        identity.SetAudiences(WellKnownAudiences.Chronicle);

    /// <summary>
    /// Sets the author identity for a Chronicle password sign-in.
    /// </summary>
    /// <param name="identity">The token identity.</param>
    /// <param name="user">The signed-in user.</param>
    public static void SetChronicleUser(this ClaimsIdentity identity, User user)
    {
        identity.AddClaim(ChronicleClaims.PrincipalKind, ChronicleClaims.User);
        identity.AddClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        identity.AddClaim(OpenIddictConstants.Claims.Name, user.Username);
    }
}
