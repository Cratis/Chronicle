// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Decides administrator-protected capabilities by delegating to the authoritative administrator check.
/// </summary>
/// <param name="administrators">The administrator check.</param>
/// <param name="options">The server configuration.</param>
/// <param name="logger">The logger.</param>
public class AuthorizationDecisions(
    IAdministratorAuthorization administrators,
    IOptions<ChronicleOptions> options,
    ILogger<AuthorizationDecisions> logger) : IAuthorizationDecisions
{
    /// <inheritdoc/>
    public async Task<AuthorizationDecision> Decide(ClaimsPrincipal? principal, Capability capability, AuthorizationScope scope)
    {
        try
        {
            if (!IsKnown(capability)) return AuthorizationDecision.Deny(AuthorizationReason.UnknownCapability, capability, scope);
            if (IsUnenrolled(capability, scope)) return AuthorizationDecision.Allow(AuthorizationReason.NotEnrolled, capability, scope);
            if (!options.Value.Authentication.Enabled) return AuthorizationDecision.Allow(AuthorizationReason.AuthenticationDisabled, capability, scope);
            if (principal?.Identity?.IsAuthenticated is not true) return AuthorizationDecision.Deny(AuthorizationReason.NoPrincipal, capability, scope);
            if (WellKnownCapabilities.Administrator.Contains(capability))
            {
                return await administrators.IsAuthorized(principal)
                    ? AuthorizationDecision.Allow(AuthorizationReason.Granted, capability, scope)
                    : AuthorizationDecision.Deny(AuthorizationReason.NotGranted, capability, scope);
            }
            return AuthorizationDecision.Deny(AuthorizationReason.NotGranted, capability, scope);
        }
        catch (Exception exception)
        {
            logger.AuthorizationDecisionFailed(exception, capability?.Value ?? string.Empty);
            return AuthorizationDecision.Deny(AuthorizationReason.Failed, capability!, scope);
        }
    }

    static bool IsKnown(Capability capability) => capability is not null && WellKnownCapabilities.Administrator.Contains(capability);

    static bool IsUnenrolled(Capability capability, AuthorizationScope scope) =>
        capability == WellKnownCapabilities.OperateEventStore
        && scope.EventStore is not null
        && !string.Equals(scope.EventStore.Value, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase);
}
