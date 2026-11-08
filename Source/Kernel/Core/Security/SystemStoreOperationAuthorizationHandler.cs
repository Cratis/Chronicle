// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Applies the operational policy only to System, auditing denials unless enforcement is enabled.
/// </summary>
/// <param name="decisions">The authorization decision point.</param>
/// <param name="options">The server configuration.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class SystemStoreOperationAuthorizationHandler(
    IAuthorizationDecisions decisions,
    IOptions<ChronicleOptions> options,
    ILogger<SystemStoreOperationAuthorizationHandler> logger) : AuthorizationHandler<SystemStoreOperationRequirement>
{
    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SystemStoreOperationRequirement requirement)
    {
        var operation = context.Resource is CommandContext commandContext ? commandContext.Command : context.Resource;
        var store = operation?.GetType().GetProperty("EventStore")?.GetValue(operation);
        var storeName = store is EventStoreName name ? name.Value : store as string;
        var scope = storeName is null ? AuthorizationScope.Kernel : AuthorizationScope.ForEventStore(storeName);
        var decision = await decisions.DecideForResource(context, WellKnownCapabilities.OperateEventStore, scope);
        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
            return;
        }

        // A decision that could not be established is never audit-allowed: it fails closed.
        if (decision.Reason == AuthorizationReason.Failed) return;
        if (options.Value.Authentication.EnforceAdministratorPolicyForOperations) return;
        if (options.Value.Authentication.EnforceDestructiveOperations && IsDestructive(operation))
        {
            logger.DestructiveOperationRefused(CallerKind(context.User), operation!.GetType().Name);
            return;
        }

        logger.AdministratorOperationWouldDeny(CallerKind(context.User), operation?.GetType().Name ?? "unknown");
        context.Succeed(requirement);
    }

    static bool IsDestructive(object? operation) => operation?.GetType().IsDefined(typeof(DestructiveOperationAttribute), false) is true;

    static string CallerKind(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated is not true) return "anonymous";
        return user.HasClaim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User) ? "user" : "app";
    }
}
