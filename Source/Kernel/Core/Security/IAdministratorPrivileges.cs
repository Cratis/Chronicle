// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Defines access to authoritative administrator privileges.
/// </summary>
public interface IAdministratorPrivileges
{
    /// <summary>
    /// Checks whether a user currently holds an active administrator privilege.
    /// </summary>
    /// <param name="userId">The user to check.</param>
    /// <returns>Whether the user is an active administrator.</returns>
    Task<bool> IsActiveAdministrator(UserId userId);
}
