// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the read model for the initial admin password setup status.
/// </summary>
/// <param name="IsRequired">Whether initial admin password setup is required.</param>
/// <param name="AdminUserId">The admin user ID if setup is required.</param>
[ReadModel]
[AllowAnonymous]
[BelongsTo(WellKnownServices.Users)]
public record AdminPasswordStatus(
    bool IsRequired,
    Guid? AdminUserId)
{
    /// <summary>
    /// Gets the initial admin password setup status.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read users from.</param>
    /// <returns>The current <see cref="AdminPasswordStatus"/>.</returns>
    internal static async Task<AdminPasswordStatus> GetStatus(IStorage storage)
    {
        var users = await storage.System.Users.GetAll();
        var adminUser = users.FirstOrDefault(u => u.Username == "admin" && !u.HasLoggedIn);
        return new AdminPasswordStatus(adminUser is not null, adminUser is not null ? (Guid)adminUser.Id : null);
    }
}
