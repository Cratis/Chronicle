// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Evaluates the administrator requirement using the authoritative check.
/// </summary>
/// <param name="authorization">The administrator authorization check.</param>
public class AdministratorAuthorizationHandler(IAdministratorAuthorization authorization) : AuthorizationHandler<AdministratorRequirement>
{
    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministratorRequirement requirement)
    {
        if (await authorization.IsAuthorized(context.User)) context.Succeed(requirement);
    }
}
