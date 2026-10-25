// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Serializes mutations of the system administrator roster.
/// </summary>
public interface IAdministratorRoster : IGrainWithStringKey
{
    /// <summary>
    /// Grants a privilege to an existing active user.
    /// </summary>
    /// <param name="userId">The user receiving the privilege.</param>
    /// <param name="reason">The authority for the grant.</param>
    /// <param name="evidence">The supporting audit evidence.</param>
    /// <returns>Awaitable task.</returns>
    Task Grant(UserId userId, GrantReason reason, string evidence);

    /// <summary>
    /// Revokes a privilege unless it is the last active administrator.
    /// </summary>
    /// <param name="userId">The user losing the privilege.</param>
    /// <param name="reason">The reason for revocation.</param>
    /// <returns>False if this would remove the last active administrator.</returns>
    Task<bool> Revoke(UserId userId, string reason);

    /// <summary>
    /// Atomically revokes a privilege and removes its user.
    /// </summary>
    /// <param name="userId">The user to remove.</param>
    /// <returns>False if this would remove the last active administrator.</returns>
    Task<bool> RemoveUser(UserId userId);

    /// <summary>
    /// Checks the authoritative roster and the user's active status.
    /// </summary>
    /// <param name="userId">The user to check.</param>
    /// <returns>Whether the user is an active administrator.</returns>
    Task<bool> IsActiveAdministrator(UserId userId);

    /// <summary>
    /// Runs the once-only upgrade migration and server-configured recovery at startup.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task EnsureAdministratorPrivilege();
}
