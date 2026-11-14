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
/// Revokes the policy of a hosted federation subject for good, so its links are refused at once. Other subjects are not affected.
/// </summary>
/// <param name="Subject">The opaque subject.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record RevokeFederationPolicy(string Subject)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="policies">The <see cref="IFederationPolicies"/> to revoke the policy with.</param>
    /// <param name="principalAccessor">The authenticated caller.</param>
    /// <param name="options">The server configuration.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IFederationPolicies policies, ICurrentPrincipalAccessor principalAccessor, IOptions<ChronicleOptions> options) =>
        policies.Revoke(Subject, PrincipalKey.Actor(principalAccessor.Current, options.Value.Authentication));
}
