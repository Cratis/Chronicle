// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Checks the authenticated caller against the authoritative administrator roster.
/// </summary>
public interface IAdministratorAuthorization
{
    /// <summary>
    /// Checks administrator authority, failing closed when it cannot be established.
    /// </summary>
    /// <param name="principal">The caller.</param>
    /// <returns>Whether the caller may administer Chronicle.</returns>
    Task<bool> IsAuthorized(ClaimsPrincipal principal);
}
