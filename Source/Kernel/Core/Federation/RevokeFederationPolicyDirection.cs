// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Revokes one direction of the policy of a hosted federation subject, so links in that direction are refused at once while the other
/// direction keeps running. Other subjects are not affected.
/// </summary>
/// <param name="Subject">The opaque subject.</param>
/// <param name="Direction">The direction to revoke, seen from this kernel.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record RevokeFederationPolicyDirection(string Subject, FederationDirection Direction)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="policies">The <see cref="IFederationPolicies"/> to revoke the direction with.</param>
    /// <param name="principalAccessor">The authenticated caller.</param>
    /// <param name="options">The server configuration.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IFederationPolicies policies, ICurrentPrincipalAccessor principalAccessor, IOptions<ChronicleOptions> options) =>
        policies.RevokeDirection(Subject, Direction, PrincipalKey.Actor(principalAccessor.Current, options.Value.Authentication));
}
