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
    public static Concepts.Identities.Identity ToIdentity(this System.Security.Claims.ClaimsPrincipal? user)
    {
        if (user == null || user.Identity?.IsAuthenticated == false)
        {
            return new Concepts.Identities.Identity("anonymous", "Anonymous", "anonymous");
        }

        var subject = user.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? Guid.NewGuid().ToString();
        var name = user.Identity?.Name ?? subject;
        var userName = user.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? name;

        return new Concepts.Identities.Identity(subject, name, userName);
    }
}
