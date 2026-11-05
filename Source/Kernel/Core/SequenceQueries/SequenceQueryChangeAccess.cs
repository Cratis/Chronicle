// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Decides whether the current caller may change a saved query or folder that is already stored.
/// </summary>
/// <remarks>
/// A stored item may be changed by the owner it was saved under, compared exactly, or by a caller
/// holding administrator authority. Anything else is refused, whatever scope the item was shared with.
/// </remarks>
static class SequenceQueryChangeAccess
{
    /// <summary>
    /// The message used when a change is refused.
    /// </summary>
    public const string QueryRefusal = "The saved query belongs to another owner.";

    /// <summary>
    /// The message used when a folder change is refused.
    /// </summary>
    public const string FolderRefusal = "The saved query folder belongs to another owner.";

    /// <summary>
    /// Check whether the current caller may change an item.
    /// </summary>
    /// <param name="storedOwner">The owner the item is stored under, or null when the item does not exist yet.</param>
    /// <param name="currentPrincipalAccessor">The <see cref="ICurrentPrincipalAccessor"/> for resolving the caller.</param>
    /// <param name="administratorAuthorization">The <see cref="IAdministratorAuthorization"/> for the administrator override.</param>
    /// <returns>True if the change is permitted, false if not.</returns>
    public static async Task<bool> IsPermitted(
        string? storedOwner,
        ICurrentPrincipalAccessor currentPrincipalAccessor,
        IAdministratorAuthorization administratorAuthorization)
    {
        if (storedOwner is null) return true;
        if (string.Equals(storedOwner, SequenceQueryOwners.GetCurrent(currentPrincipalAccessor), StringComparison.Ordinal)) return true;

        var principal = currentPrincipalAccessor.Current;
        return principal is not null && await administratorAuthorization.IsAuthorized(principal);
    }
}
