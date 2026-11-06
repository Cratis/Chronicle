// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Grpc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Decides the capability a query exercises at the event store it addresses, the same way for every transport.
/// </summary>
public static class StoreCapabilityDecision
{
    static readonly Dictionary<string, string> _capabilities = typeof(StoreCapabilityDecision).Assembly.GetTypes()
        .Select(type => (type.FullName, Attribute: type.GetCustomAttribute<StoreCapabilityAttribute>(false)))
        .Where(_ => _.FullName is not null && _.Attribute is not null)
        .ToDictionary(_ => _.FullName!, _ => _.Attribute!.Capability, StringComparer.Ordinal);

    /// <summary>
    /// Gets the capability a query declares.
    /// </summary>
    /// <param name="queryName">The fully qualified name of the query: the read model, optionally followed by the query method.</param>
    /// <returns>The capability, or null if the query declares none.</returns>
    public static string? CapabilityOf(string queryName)
    {
        if (_capabilities.TryGetValue(queryName, out var capability)) return capability;

        var separator = queryName.LastIndexOf('.');
        return separator > 0 && _capabilities.TryGetValue(queryName[..separator], out capability) ? capability : null;
    }

    /// <summary>
    /// Decides whether a caller holds every kind of access a call needs at the event store (and namespace) it addresses.
    /// </summary>
    /// <param name="services">The services of the call.</param>
    /// <param name="user">The caller, or null when there is none.</param>
    /// <param name="access">The access the call needs.</param>
    /// <param name="eventStore">The addressed event store.</param>
    /// <param name="namespace">The addressed namespace, if any.</param>
    /// <returns>True if the call may run.</returns>
    public static async Task<bool> PermitsAccess(IServiceProvider services, ClaimsPrincipal? user, StoreAccess access, string eventStore, string? @namespace)
    {
        for (var single = StoreAccess.Read; single <= StoreAccess.Manage; single = (StoreAccess)((int)single << 1))
        {
            if ((access & single) == StoreAccess.None) continue;
            if (!await Permits(services, user, StoreAccessMap.CapabilityFor(single).Value, eventStore, @namespace)) return false;
        }

        return true;
    }

    /// <summary>
    /// Decides whether a caller may exercise a capability at the event store (and namespace) a query addresses.
    /// </summary>
    /// <param name="services">The services of the call.</param>
    /// <param name="user">The caller, or null when there is none.</param>
    /// <param name="capability">The capability the query exercises.</param>
    /// <param name="eventStore">The addressed event store.</param>
    /// <param name="namespace">The addressed namespace, if any.</param>
    /// <returns>True if the query may run.</returns>
    /// <remarks>
    /// The decision is made through <see cref="IAuthorizationDecisions"/>, so a per-store entitlement takes effect without
    /// the queries changing. Until then a caller that is not granted a known capability at an ordinary event store is allowed
    /// and the decision is recorded; a decision that failed, a capability that is not known, and a decision that cannot be made are refused.
    /// </remarks>
    public static async Task<bool> Permits(IServiceProvider services, ClaimsPrincipal? user, string capability, string eventStore, string? @namespace)
    {
        AuthorizationDecision decision;
        StoreRequestDecisions? memo = null;
        var authentication = services.GetService<IOptions<ChronicleOptions>>()?.Value.Authentication ?? new Authentication();
        try
        {
            var decisions = services.GetRequiredService<IAuthorizationDecisions>();
            var cache = services.GetService<IStoreEntitlementsCache>();
            var request = services.GetService<IHttpContextAccessor>()?.HttpContext;
            if (decisions is AuthorizationDecisions && cache is not null && request is not null && IsOrdinary(capability, eventStore)) memo = StoreRequestDecisions.For(request);
            var cached = cache is null ? null : memo?.Find((user, capability, eventStore, @namespace), cache, authentication);
            if (cached is not null)
            {
                decision = cached;
            }
            else
            {
                var snapshot = cache?.Snapshot;
                var fresh = cache?.IsFresh is true;
                var scope = @namespace is null ? AuthorizationScope.ForEventStore(eventStore) : AuthorizationScope.ForNamespace(eventStore, @namespace);
                decision = await decisions.Decide(user, new(capability), scope);
                if (snapshot is not null) memo?.Remember((user, capability, eventStore, @namespace), (snapshot, fresh), authentication, decision);
            }
        }
        catch
        {
            return false;
        }

        var permitted = StoreEntitlementEnforcement.Permits(decision, authentication.EnforceStoreEntitlements);
        var loggers = services.GetRequiredService<ILoggerFactory>();
        if (!decision.IsAllowed && !permitted) loggers.CreateLogger(typeof(StoreCapabilityDecision).FullName!).Decided(capability, decision.Reason.ToString(), permitted);
        if (memo?.TakeAudit(decision) is not false) StoreEntitlementEnforcement.Audit(decision, user, authentication, loggers, services.GetService<IGrainFactory>());
        return permitted;
    }

    static bool IsOrdinary(string capability, string store) => !string.Equals(store, "System", StringComparison.OrdinalIgnoreCase)
        && (capability == WellKnownCapabilities.ReadEventStore.Value || capability == WellKnownCapabilities.AppendEventStore.Value || capability == WellKnownCapabilities.ManageEventStore.Value);
}
