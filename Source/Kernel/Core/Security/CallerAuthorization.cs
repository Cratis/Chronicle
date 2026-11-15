// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Security;

/// <summary>Routes caller providers to the default decision point while preserving host overrides.</summary>
internal static class CallerAuthorization
{
    /// <summary>Decides using the supplied caller provider.</summary>
    /// <param name="decisions">The decision point.</param>
    /// <param name="caller">The caller and provider.</param>
    /// <param name="capability">The capability.</param>
    /// <param name="scope">The authorization scope.</param>
    /// <returns>The decision.</returns>
    internal static Task<AuthorizationDecision> Decide(this IAuthorizationDecisions decisions, AuthorizationCaller caller, Capability capability, AuthorizationScope scope) =>
        decisions is AuthorizationDecisions local
            ? local.Decide(caller.Principal, capability, scope, caller.Services)
            : decisions.Decide(caller.Principal, capability, scope);

    /// <summary>Uses the resource's provider when supplied by a transport.</summary>
    /// <param name="decisions">The decision point.</param>
    /// <param name="context">The authorization context.</param>
    /// <param name="capability">The capability.</param>
    /// <param name="scope">The authorization scope.</param>
    /// <returns>The decision.</returns>
    internal static Task<AuthorizationDecision> DecideForResource(this IAuthorizationDecisions decisions, AuthorizationHandlerContext context, Capability capability, AuthorizationScope scope) => context.Resource switch
    {
        CommandContext command => decisions.Decide(new AuthorizationCaller(context.User, command.ServiceProvider), capability, scope),
        QueryContext query => decisions.Decide(new AuthorizationCaller(context.User, query.ServiceProvider), capability, scope),
        HttpContext http => decisions.Decide(new AuthorizationCaller(context.User, http.RequestServices), capability, scope),
        _ => decisions.Decide(context.User, capability, scope)
    };

    /// <summary>Checks System access using the caller provider.</summary>
    /// <param name="access">The access check.</param>
    /// <param name="principal">The caller.</param>
    /// <param name="services">The caller's provider.</param>
    /// <returns>Whether access is allowed.</returns>
    internal static Task<bool> IsAllowed(this ISystemStoreAccess access, ClaimsPrincipal? principal, IServiceProvider? services) =>
        access is SystemStoreAccessVerdict local
            ? local.IsAllowed(principal, services)
            : access.IsAllowed(principal);
}
