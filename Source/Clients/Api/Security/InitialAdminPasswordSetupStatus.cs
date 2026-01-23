// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the status of whether initial admin password setup is required.
/// </summary>
/// <param name="IsRequired">Whether initial admin password setup is required.</param>
/// <param name="AdminUserId">The admin user ID if setup is required.</param>
[ReadModel]
[AllowAnonymous]
public record InitialAdminPasswordSetupStatus(
    bool IsRequired,
    Guid? AdminUserId)
{
    /// <summary>
    /// Gets the initial admin password setup status.
    /// </summary>
    /// <param name="users">The <see cref="IUsers"/> contract.</param>
    /// <returns>The <see cref="InitialAdminPasswordSetupStatus"/>.</returns>
    internal static async Task<InitialAdminPasswordSetupStatus> GetStatus(IUsers users)
    {
        var status = await users.GetInitialAdminPasswordSetupStatus();
        return new InitialAdminPasswordSetupStatus(status.IsRequired, status.AdminUserId);
    }
}
