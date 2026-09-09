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
/// <param name="AdminUsername">The administrator username when setup is required.</param>
[ReadModel]
[AllowAnonymous]
[BelongsTo(WellKnownServices.Users)]
public record AdminPasswordStatus(
    bool IsRequired,
    Guid? AdminUserId,
    string AdminUsername)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminPasswordStatus"/> record without username information.
    /// </summary>
    /// <param name="isRequired">Whether setup is required.</param>
    /// <param name="adminUserId">The administrator identifier.</param>
    public AdminPasswordStatus(bool isRequired, Guid? adminUserId) : this(isRequired, adminUserId, string.Empty)
    {
    }

    /// <summary>
    /// Gets the initial admin password setup status.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read users from.</param>
    /// <param name="options">The configured administrator.</param>
    /// <returns>The current <see cref="AdminPasswordStatus"/>.</returns>
    internal static async Task<AdminPasswordStatus> GetStatus(IStorage storage, IOptions<Configuration.ChronicleOptions> options)
    {
        var adminUser = await storage.System.Users.GetByUsername(options.Value.Authentication.EffectiveAdminUsername);
        var required = adminUser is { HasLoggedIn: false } && string.IsNullOrEmpty(adminUser.PasswordHash?.Value);
        return new AdminPasswordStatus(required, required ? (Guid)adminUser!.Id : null, required ? adminUser!.Username : string.Empty);
    }
}
