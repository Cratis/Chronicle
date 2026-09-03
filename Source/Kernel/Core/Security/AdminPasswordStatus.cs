// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the read model for the initial admin password setup status.
/// </summary>
/// <param name="IsRequired">Whether initial admin password setup is required.</param>
/// <param name="AdminUserId">The admin user ID if setup is required.</param>
/// <param name="AdminUsername">The configured administrator username if setup is required.</param>
[ReadModel]
[AllowAnonymous]
[BelongsTo(WellKnownServices.Users)]
public record AdminPasswordStatus(
    bool IsRequired,
    Guid? AdminUserId,
    string AdminUsername)
{
    /// <summary>
    /// Gets the initial admin password setup status.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read users from.</param>
    /// <param name="options">The Chronicle options identifying the configured administrator.</param>
    /// <returns>The current <see cref="AdminPasswordStatus"/>.</returns>
    internal static async Task<AdminPasswordStatus> GetStatus(IStorage storage, IOptions<Configuration.ChronicleOptions> options)
    {
        var administratorUsername = options.Value.Authentication.EffectiveAdminUsername;
        var users = await storage.System.Users.GetAll();
        var adminUser = users.FirstOrDefault(user => user.Username == administratorUsername && !user.HasLoggedIn);
        return new AdminPasswordStatus(
            adminUser is not null,
            adminUser is not null ? (Guid)adminUser.Id : null,
            adminUser is not null ? administratorUsername : string.Empty);
    }
}
