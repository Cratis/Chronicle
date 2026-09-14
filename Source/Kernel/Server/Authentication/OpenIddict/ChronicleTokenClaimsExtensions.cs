// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using OpenIddict.Abstractions;

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
}
