// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Revokes an enrollment, and every certificate issued from it, so the peer is refused at once.
/// </summary>
/// <param name="Id">The enrollment id.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record RevokeFederationEnrollment(string Id)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the federation authority with.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IGrainFactory grainFactory) =>
        grainFactory.GetGrain<IFederationAuthority>(IFederationAuthority.Key).RevokeEnrollment(Id);
}
