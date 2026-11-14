// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Redeems an enrollment code on this kernel, so it holds a certificate issued by the hosted federation authority. No restart is needed.
/// </summary>
/// <remarks>
/// Idempotent when this kernel is already enrolled with the same authority. The code is a one-time secret: it is never logged
/// or recorded, and the key pair it enrolls is generated on this kernel.
/// </remarks>
/// <param name="Code">The enrollment code handed over by the operator.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record RedeemFederationEnrollment(string Code)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="enrollment">The <see cref="IFederationPeerEnrollment"/> to redeem the code with.</param>
    /// <returns>The identity this kernel now holds.</returns>
    public Task<FederationRedemption> Handle(IFederationPeerEnrollment enrollment) => enrollment.Redeem(Code);

    /// <inheritdoc/>
    public override string ToString() => "RedeemFederationEnrollment { Code = *** }";
}
