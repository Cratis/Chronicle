// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents an active administrator from the authoritative roster.
/// </summary>
/// <param name="UserId">The privileged user.</param>
/// <param name="Username">The user's name.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Administrators)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record Administrator(UserId UserId, string Username)
{
    /// <summary>
    /// Gets active administrator identifiers from the roster, joined with user names.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="storage">The system user storage.</param>
    /// <returns>The active administrators.</returns>
    public static async Task<IEnumerable<Administrator>> AllAdministrators(IGrainFactory grainFactory, IStorage storage)
    {
        var ids = await grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).GetActiveAdministrators();
        var administrators = new List<Administrator>();
        foreach (var id in ids)
        {
            var user = await storage.System.Users.GetById(id);
            if (user?.IsActive is true) administrators.Add(new(id, user.Username.Value));
        }
        return administrators;
    }
}
