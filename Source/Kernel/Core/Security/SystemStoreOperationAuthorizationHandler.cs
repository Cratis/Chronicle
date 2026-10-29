// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Applies the operational policy only to System, auditing denials unless enforcement is enabled.
/// </summary>
/// <param name="authorization">The administrator check.</param>
/// <param name="options">The server configuration.</param>
/// <param name="logger">The logger.</param>
public class SystemStoreOperationAuthorizationHandler(
    IAdministratorAuthorization authorization,
    IOptions<ChronicleOptions> options,
    ILogger<SystemStoreOperationAuthorizationHandler> logger) : AuthorizationHandler<SystemStoreOperationRequirement>
{
    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SystemStoreOperationRequirement requirement)
    {
        var operation = context.Resource is CommandContext commandContext ? commandContext.Command : context.Resource;
        var store = operation?.GetType().GetProperty("EventStore")?.GetValue(operation);
        var storeName = store is EventStoreName name ? name.Value : store as string;
        if (storeName is not null && !string.Equals(storeName, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }
        if (await authorization.IsAuthorized(context.User))
        {
            context.Succeed(requirement);
            return;
        }
        if (options.Value.Authentication.EnforceAdministratorPolicyForOperations) return;
        logger.AdministratorOperationWouldDeny(CallerKind(context.User), operation?.GetType().Name ?? "unknown");
        context.Succeed(requirement);
    }

    static string CallerKind(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated is not true) return "anonymous";
        return user.HasClaim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User) ? "user" : "app";
    }
}
