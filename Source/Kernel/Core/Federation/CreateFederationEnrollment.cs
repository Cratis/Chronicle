// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Creates a single-use, expiring enrollment for an opaque subject, and returns the code to hand to the customer.
/// </summary>
/// <remarks>
/// The code is a one-time secret that is shown only in this result. Only a hash of its token is stored.
/// </remarks>
/// <param name="Subject">The opaque subject the enrollment is for. It becomes the customer part of the identity.</param>
/// <param name="Instance">The kernel instance the identity is issued for. Empty to generate one.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record CreateFederationEnrollment(string Subject, string Instance)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the federation authority with.</param>
    /// <returns>The enrollment and its code.</returns>
    public Task<FederationEnrollmentCreatedResult> Handle(IGrainFactory grainFactory) =>
        grainFactory.GetGrain<IFederationAuthority>(IFederationAuthority.Key).CreateEnrollment(Subject, Instance);
}
