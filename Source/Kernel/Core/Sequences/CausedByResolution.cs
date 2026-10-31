// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Resolves event authorship for sequence commands.
/// </summary>
internal static class CausedByResolution
{
    /// <summary>
    /// Resolves the author from the caller and the optional supplied identity.
    /// </summary>
    /// <param name="caller">The authenticated caller, if any.</param>
    /// <param name="supplied">The optional on-behalf-of identity.</param>
    /// <returns>The identity to record as the author.</returns>
    internal static Concepts.Identities.Identity ResolveCausedBy(this ClaimsPrincipal? caller, Identity? supplied) =>
        caller?.Identities.Any(identity => identity.IsAuthenticated && identity.HasClaim(ChronicleClaims.PrincipalKind, ChronicleClaims.User)) == true
            ? caller.ToIdentity()
            : supplied?.ToChronicle() ?? caller.ToIdentity();
}
