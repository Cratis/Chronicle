// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Decides the federation management capability through the authorization decision point.
/// </summary>
/// <param name="decisions">The authorization decision point.</param>
[Singleton]
public class FederationManagementAuthorizationHandler(IAuthorizationDecisions decisions) : AuthorizationHandler<FederationManagementRequirement>
{
    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, FederationManagementRequirement requirement)
    {
        var decision = await decisions.DecideForResource(context, WellKnownCapabilities.ManageFederation, AuthorizationScope.Kernel);
        if (decision.IsAllowed) context.Succeed(requirement);
    }
}
