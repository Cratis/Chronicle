// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Revokes a producer link so it stops delivering. Other links are unaffected.
/// </summary>
/// <param name="Id">The link id.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record RevokeFederationLink(string Id)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="registry">The <see cref="IFederationRegistry"/> to revoke with.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IFederationRegistry registry) => registry.Revoke(Id);
}
