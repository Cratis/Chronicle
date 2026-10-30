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
/// Grants administrator privilege to an existing active user.
/// </summary>
/// <param name="UserId">The user receiving the grant.</param>
[Command]
[BelongsTo(WellKnownServices.Administrators)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record GrantAdministrator(UserId UserId)
{
    /// <summary>
    /// Records an audited grant through the guarded authoritative roster.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="principalAccessor">The authenticated caller.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IGrainFactory grainFactory, ICurrentPrincipalAccessor principalAccessor)
    {
        var principal = principalAccessor.Current;
        var actor = principal?.FindFirst("sub")?.Value ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        return grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key)
            .Grant(UserId, GrantReason.AdministratorGrant, JsonSerializer.Serialize(new { Actor = actor }));
    }
}
