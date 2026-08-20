// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Resolves the identity that owns saved event sequence queries.
/// </summary>
/// <remarks>
/// The owner is always taken from the principal executing the request, never from the client - a
/// caller must not be able to read or overwrite somebody else's private queries by naming them.
/// </remarks>
public static class SequenceQueryOwners
{
    /// <summary>
    /// The owner recorded for queries saved without an authenticated principal, which is the normal
    /// case when the workbench runs against a locally hosted development kernel.
    /// </summary>
    public const string Anonymous = "anonymous";

    /// <summary>
    /// Get the owner for the principal currently executing.
    /// </summary>
    /// <param name="currentPrincipalAccessor">The <see cref="ICurrentPrincipalAccessor"/> to resolve from.</param>
    /// <returns>The owner identifier.</returns>
    public static string GetCurrent(ICurrentPrincipalAccessor currentPrincipalAccessor)
    {
        var principal = currentPrincipalAccessor.Current;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Anonymous;
        }

        return principal.Claims.FirstOrDefault(_ => _.Type == "sub")?.Value
            ?? principal.Identity.Name
            ?? Anonymous;
    }
}
