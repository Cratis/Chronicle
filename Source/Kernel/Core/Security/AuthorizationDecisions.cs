// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Decides explicit store access locally and administrator capabilities authoritatively.
/// </summary>
/// <param name="options">The server configuration.</param>
/// <param name="logger">The logger.</param>
/// <param name="cache">The local silo's explicit grant snapshot.</param>
/// <param name="httpContextAccessor">The current HTTP caller, if any.</param>
[Singleton]
public class AuthorizationDecisions(
    IOptions<ChronicleOptions> options,
    ILogger<AuthorizationDecisions> logger,
    IStoreEntitlementsCache? cache = null,
    IHttpContextAccessor? httpContextAccessor = null) : IAuthorizationDecisions
{
    static StoreDecisionInputs? _fallbackInputs;

    /// <summary>Gets immutable inputs for the current snapshot.</summary>
    internal StoreDecisionInputs Inputs => cache?.Snapshot.Inputs ?? LazyInitializer.EnsureInitialized(ref _fallbackInputs);

    /// <inheritdoc/>
    public Task<AuthorizationDecision> Decide(ClaimsPrincipal? principal, Capability capability, AuthorizationScope scope) =>
        Decide(principal, capability, scope, httpContextAccessor?.HttpContext?.RequestServices);

    /// <summary>Decides using the caller's provider without retaining its services.</summary>
    /// <param name="principal">The caller.</param>
    /// <param name="capability">The capability.</param>
    /// <param name="scope">The authorization scope.</param>
    /// <param name="services">The caller's provider, or null when unavailable.</param>
    /// <returns>The decision.</returns>
    [SuppressMessage("Roslynator", "RCS1229:Use async/await when necessary", Justification = "Ordinary store decisions must complete synchronously; the cold administrator branch handles its asynchronous failures.")]
    internal Task<AuthorizationDecision> Decide(ClaimsPrincipal? principal, Capability capability, AuthorizationScope scope, IServiceProvider? services)
    {
        try
        {
            if (!IsKnown(capability)) return Deny(AuthorizationReason.UnknownCapability, capability, scope);
            if (IsUnenrolled(capability, scope)) return Allow(AuthorizationReason.NotEnrolled, capability, scope);
            if (!options.Value.Authentication.Enabled) return Allow(AuthorizationReason.AuthenticationDisabled, capability, scope);
            if (principal?.Identity?.IsAuthenticated is not true) return Deny(AuthorizationReason.NoPrincipal, capability, scope);
            return IsOrdinaryStoreScope(capability, scope)
                ? DecideStore(principal, capability, scope, services)
                : DecideAdministrator(principal, capability, scope, services);
        }
        catch (Exception exception)
        {
            logger.AuthorizationDecisionFailed(exception, capability?.Value ?? string.Empty);
            return Deny(AuthorizationReason.Failed, capability!, scope);
        }
    }

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

    Task<AuthorizationDecision> Allow(AuthorizationReason reason, Capability capability, AuthorizationScope scope) => Inputs.Result(reason, capability, scope, true);
    Task<AuthorizationDecision> Deny(AuthorizationReason reason, Capability capability, AuthorizationScope scope) => Inputs.Result(reason, capability, scope, false);

    Task<AuthorizationDecision> DecideStore(ClaimsPrincipal principal, Capability capability, AuthorizationScope scope, IServiceProvider? services)
    {
        var authentication = options.Value.Authentication;
        var holder = PrincipalKey.From(principal, authentication);
        if (holder.IsUnidentified)
        {
            return IsOrdinaryCapability(capability)
                ? Deny(AuthorizationReason.NoPrincipal, capability, scope)
                : DecideAdministrator(principal, capability, scope, services);
        }
        if (authentication.EnforceStoreEntitlements && cache?.IsFresh is not true) return Deny(AuthorizationReason.Failed, capability, scope);
        if (HasGrant(holder, principal, capability, scope)) return Allow(AuthorizationReason.Granted, capability, scope);
        if (!IsOrdinaryCapability(capability)) return DecideAdministrator(principal, capability, scope, services);
        return PrincipalKey.IsLocalUser(principal, authentication)
            ? Allow(AuthorizationReason.NotEnrolled, capability, scope)
            : Deny(AuthorizationReason.NotGranted, capability, scope);
    }

    bool HasCachedGrant(PrincipalKey holder, Capability capability, AuthorizationScope scope)
    {
        var snapshot = cache?.Snapshot;
        if (snapshot is null || !cache!.IsFresh) return false;
        if (!snapshot.Allows(holder, scope.EventStore!, scope.Namespace, WellKnownCapabilities.AccessOf(capability))) return false;
        return ReferenceEquals(snapshot, cache.Snapshot) && cache.IsFresh;
    }

    bool HasGrant(PrincipalKey holder, ClaimsPrincipal principal, Capability capability, AuthorizationScope scope)
    {
        if (HasCachedGrant(holder, capability, scope)) return true;
        return holder.Issuer != PrincipalKey.LocalIssuer && ExternalAdministratorClaims.Grants(principal, options.Value.Authentication.ExternalAdministratorClaim);
    }

    async Task<AuthorizationDecision> DecideAdministrator(ClaimsPrincipal principal, Capability capability, AuthorizationScope scope, IServiceProvider? services)
    {
        try
        {
            if (services is null) return AuthorizationDecision.Deny(AuthorizationReason.Failed, capability, scope);
            var administrators = services.GetRequiredService<IAdministratorAuthorization>();
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
