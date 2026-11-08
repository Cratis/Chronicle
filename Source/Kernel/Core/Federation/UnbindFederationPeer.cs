// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Removes a consumer binding.
/// </summary>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record UnbindFederationPeer(string Id)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task Handle() => throw new NotImplementedException();
}
