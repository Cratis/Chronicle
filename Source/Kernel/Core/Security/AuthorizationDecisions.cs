// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Decides explicit store access locally and administrator capabilities authoritatively.
/// </summary>
/// <param name="administrators">The administrator check.</param>
/// <param name="options">The server configuration.</param>
/// <param name="logger">The logger.</param>
/// <param name="cache">The local silo's explicit grant snapshot.</param>
public class AuthorizationDecisions(
    IAdministratorAuthorization administrators,
    IOptions<ChronicleOptions> options,
    ILogger<AuthorizationDecisions> logger,
    IStoreEntitlementsCache? cache = null) : IAuthorizationDecisions
{
    /// <inheritdoc/>
    [SuppressMessage("Roslynator", "RCS1229:Use async/await when necessary", Justification = "Ordinary store decisions must complete synchronously; the cold administrator branch handles its asynchronous failures.")]
    public Task<AuthorizationDecision> Decide(ClaimsPrincipal? principal, Capability capability, AuthorizationScope scope)
    {
        try
        {
            if (!IsKnown(capability)) return Deny(AuthorizationReason.UnknownCapability, capability, scope);
            if (IsUnenrolled(capability, scope)) return Allow(AuthorizationReason.NotEnrolled, capability, scope);
            if (!options.Value.Authentication.Enabled) return Allow(AuthorizationReason.AuthenticationDisabled, capability, scope);
            if (principal?.Identity?.IsAuthenticated is not true) return Deny(AuthorizationReason.NoPrincipal, capability, scope);
            return IsOrdinaryStoreScope(capability, scope)
                ? DecideStore(principal, capability, scope)
                : DecideAdministrator(principal, capability, scope);
        }
        catch (Exception exception)
        {
            logger.AuthorizationDecisionFailed(exception, capability?.Value ?? string.Empty);
            return Deny(AuthorizationReason.Failed, capability!, scope);
        }
    }

    static Task<AuthorizationDecision> Allow(AuthorizationReason reason, Capability capability, AuthorizationScope scope) => Task.FromResult(AuthorizationDecision.Allow(reason, capability, scope));
    static Task<AuthorizationDecision> Deny(AuthorizationReason reason, Capability capability, AuthorizationScope scope) => Task.FromResult(AuthorizationDecision.Deny(reason, capability, scope));

    static bool IsKnown(Capability capability) => capability is not null
        && (WellKnownCapabilities.Administrator.Contains(capability) || WellKnownCapabilities.StoreScoped.Contains(capability));

    static bool IsOrdinaryStoreScope(Capability capability, AuthorizationScope scope) => WellKnownCapabilities.StoreScoped.Contains(capability)
        && scope.EventStore is not null && !SystemStoreAccess.IsSystem(scope.EventStore);

    static bool IsOrdinaryCapability(Capability capability) => capability == WellKnownCapabilities.ReadEventStore
        || capability == WellKnownCapabilities.AppendEventStore || capability == WellKnownCapabilities.ManageEventStore;

    static bool IsUnenrolled(Capability capability, AuthorizationScope scope) =>
        capability == WellKnownCapabilities.OperateEventStore
        && scope.EventStore is not null
        && !string.Equals(scope.EventStore.Value, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase);

    Task<AuthorizationDecision> DecideStore(ClaimsPrincipal principal, Capability capability, AuthorizationScope scope)
    {
        var authentication = options.Value.Authentication;
        var holder = PrincipalKey.From(principal, authentication);
        if (holder.IsUnidentified)
        {
            return IsOrdinaryCapability(capability)
                ? Deny(AuthorizationReason.NoPrincipal, capability, scope)
                : DecideAdministrator(principal, capability, scope);
        }
        if (authentication.EnforceStoreEntitlements && cache?.IsFresh is not true) return Deny(AuthorizationReason.Failed, capability, scope);
        if (HasGrant(holder, principal, capability, scope)) return Allow(AuthorizationReason.Granted, capability, scope);
        if (!IsOrdinaryCapability(capability)) return DecideAdministrator(principal, capability, scope);
        return PrincipalKey.IsLocalUser(principal, authentication)
            ? Allow(AuthorizationReason.NotEnrolled, capability, scope)
            : Deny(AuthorizationReason.NotGranted, capability, scope);
    }

    bool HasGrant(PrincipalKey holder, ClaimsPrincipal principal, Capability capability, AuthorizationScope scope)
    {
        if ((cache?.Snapshot ?? StoreEntitlementsSnapshot.Empty).Allows(holder, scope.EventStore!, scope.Namespace, WellKnownCapabilities.AccessOf(capability))) return true;
        return holder.Issuer != PrincipalKey.LocalIssuer && ExternalAdministratorClaims.Grants(principal, options.Value.Authentication.ExternalAdministratorClaim);
    }

    async Task<AuthorizationDecision> DecideAdministrator(ClaimsPrincipal principal, Capability capability, AuthorizationScope scope)
    {
        try
        {
            return await administrators.IsAuthorized(principal)
                ? AuthorizationDecision.Allow(AuthorizationReason.Granted, capability, scope)
                : AuthorizationDecision.Deny(AuthorizationReason.NotGranted, capability, scope);
        }
        catch (Exception exception)
        {
            logger.AuthorizationDecisionFailed(exception, capability.Value);
            return AuthorizationDecision.Deny(AuthorizationReason.Failed, capability, scope);
        }
    }
}
