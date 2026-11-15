// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Decides whether the current caller may change a saved query or folder that is already stored.
/// </summary>
/// <remarks>
/// A stored item may be changed by the owner it was saved under, compared exactly, or by a caller
/// holding administrator authority. Anything else is refused, whatever scope the item was shared with.
/// The check and the write are separate storage calls, so the write itself is conditional on the stored
/// owner that was checked, and a write that loses to a concurrent change is checked once more.
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

    /// <summary>
    /// Check the stored item and write the change conditionally on the owner that was checked.
    /// </summary>
    /// <typeparam name="TStored">The type of the stored item.</typeparam>
    /// <param name="read">Reads the stored item, or null when there is none.</param>
    /// <param name="ownerOf">Gets the owner a stored item is stored under.</param>
    /// <param name="write">Writes the change given what was read, and reports whether it was applied.</param>
    /// <param name="refusal">The message used when the change is refused.</param>
    /// <param name="currentPrincipalAccessor">The <see cref="ICurrentPrincipalAccessor"/> for resolving the caller.</param>
    /// <param name="administratorAuthorization">The <see cref="IAdministratorAuthorization"/> for the administrator override.</param>
    /// <returns>Null when the change was applied, otherwise the refusal.</returns>
    /// <remarks>
    /// When the write loses to a concurrent change the item is read and checked once more. Losing a
    /// second time is a refusal - a lost write is never reported as a success.
    /// </remarks>
    public static async Task<ValidationResult?> Change<TStored>(
        Func<Task<TStored?>> read,
        Func<TStored, SequenceQueryOwner> ownerOf,
        Func<TStored?, Task<SequenceQueryWriteOutcome>> write,
        string refusal,
        ICurrentPrincipalAccessor currentPrincipalAccessor,
        IAdministratorAuthorization administratorAuthorization)
        where TStored : class
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var existing = await read();
            if (!await IsPermitted(existing is null ? null : ownerOf(existing).Value, currentPrincipalAccessor, administratorAuthorization)) break;
            if (await write(existing) == SequenceQueryWriteOutcome.Applied) return null;
        }

        return ValidationResult.Error(refusal);
    }
}
