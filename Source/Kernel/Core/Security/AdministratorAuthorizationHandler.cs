// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Evaluates the administrator requirement through the authorization decision point.
/// </summary>
/// <param name="decisions">The authorization decision point.</param>
[Singleton]
public class AdministratorAuthorizationHandler(IAuthorizationDecisions decisions) : AuthorizationHandler<AdministratorRequirement>
{
    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministratorRequirement requirement)
    {
        var decision = await decisions.DecideForResource(context, WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel);
        if (decision.IsAllowed) context.Succeed(requirement);
    }
}
