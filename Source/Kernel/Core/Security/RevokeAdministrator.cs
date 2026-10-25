// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Revokes administrator privilege without removing the user account.
/// </summary>
/// <param name="UserId">The user losing the privilege.</param>
/// <param name="Reason">The reason for revocation, not a credential.</param>
[Command]
[BelongsTo(WellKnownServices.Administrators)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record RevokeAdministrator(UserId UserId, string Reason)
{
    /// <summary>
    /// Revokes through the guarded roster, preserving its last-active-administrator safeguard.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="principalAccessor">The authenticated caller.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="LastAdministratorCannotBeRemoved">The last active administrator cannot lose the privilege.</exception>
    public async Task Handle(IGrainFactory grainFactory, ICurrentPrincipalAccessor principalAccessor)
    {
        var reason = JsonSerializer.Serialize(new
        {
            Actor = principalAccessor.Current?.FindFirst("sub")?.Value ?? principalAccessor.Current?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
            Reason
        });
        if (!await grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).Revoke(UserId, reason))
        {
            throw new LastAdministratorCannotBeRemoved();
        }
    }
}
