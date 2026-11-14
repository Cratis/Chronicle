// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Decides the federation enrollment capability through the authorization decision point.
/// </summary>
/// <param name="decisions">The authorization decision point.</param>
[Singleton]
public class FederationEnrollmentAuthorizationHandler(IAuthorizationDecisions decisions) : AuthorizationHandler<FederationEnrollmentRequirement>
{
    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, FederationEnrollmentRequirement requirement)
    {
        var decision = await decisions.DecideForResource(context, WellKnownCapabilities.EnrollFederation, AuthorizationScope.Kernel);
        if (decision.IsAllowed) context.Succeed(requirement);
    }
}
