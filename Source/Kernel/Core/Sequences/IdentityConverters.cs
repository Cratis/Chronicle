// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converters for working with <see cref="Concepts.Identities.Identity"/>.
/// </summary>
public static class IdentityConverters
{
    /// <summary>
    /// Converts a <see cref="System.Security.Claims.ClaimsPrincipal"/> to an <see cref="Concepts.Identities.Identity"/>.
    /// </summary>
    /// <param name="user">The user to convert.</param>
    /// <returns>The converted identity.</returns>
    /// <exception cref="AuthenticatedUserHasNoSubject">Thrown when an authenticated principal has no meaningful subject.</exception>
    public static Concepts.Identities.Identity ToIdentity(this System.Security.Claims.ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated is not true)
        {
            return new Concepts.Identities.Identity("anonymous", "Anonymous", "anonymous");
        }

        var subject = user.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new AuthenticatedUserHasNoSubject();
        }

        var name = user.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? user.Identity?.Name;
        name = string.IsNullOrWhiteSpace(name) ? subject : name;
        var userName = user.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        userName = string.IsNullOrWhiteSpace(userName) ? name : userName;

        return new Concepts.Identities.Identity(subject, name, userName);
    }
}
