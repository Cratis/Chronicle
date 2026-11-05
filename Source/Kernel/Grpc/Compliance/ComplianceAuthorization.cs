// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Compliance;

/// <summary>
/// Decides compliance capabilities at the addressed event store and namespace boundary.
/// </summary>
/// <param name="options">The server configuration.</param>
/// <param name="storage">The event store listing.</param>
/// <param name="scopes">Creates a scope when the caller has no request services.</param>
/// <param name="logger">The sanitized decision logger.</param>
[Singleton]
public class ComplianceAuthorization(
    IOptions<ChronicleOptions> options,
    IStorage storage,
    IServiceScopeFactory scopes,
    ILogger<ComplianceAuthorization> logger)
{
    bool Enforced => options.Value.Authentication.EnforceComplianceCapabilities;

    /// <summary>
    /// Decide whether the current caller may release protected content.
    /// </summary>
    /// <param name="eventStore">The addressed event store.</param>
    /// <param name="namespace">The addressed namespace.</param>
    /// <param name="context">The current call.</param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="RpcException">The caller may not release protected content in enforce mode.</exception>
    public async Task Release(string eventStore, string @namespace, CallContext context)
    {
        var caller = Caller(context);
        var decision = await Decide(caller, WellKnownCapabilities.ReleaseCompliance, eventStore, @namespace);
        Log(caller.Owner, "release", @namespace, [decision]);
        if (Enforced && !decision.IsAllowed)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Compliance release is not permitted."));
        }
    }

    /// <summary>
    /// Decide all key-lifecycle targets before any key state is changed.
    /// </summary>
    /// <param name="operation">The key-lifecycle operation name.</param>
    /// <param name="eventStore">The requested event store.</param>
    /// <param name="namespace">The addressed namespace.</param>
    /// <param name="context">The current call.</param>
    /// <returns>The allowed and not-covered targets.</returns>
    public async Task<ComplianceKeyCoverage> KeyLifecycle(string operation, string eventStore, string @namespace, CallContext context)
    {
        var caller = Caller(context);
        var (targets, listingFailed) = await Targets(eventStore);
        List<AuthorizationDecision> results = [];
        foreach (var target in targets)
        {
            results.Add(await Decide(caller, WellKnownCapabilities.ManageComplianceKeys, target, @namespace));
        }

        Log(caller.Owner, operation, @namespace, results, listingFailed);
        if (!Enforced) return new(targets, [], listingFailed);
        var denied = results.Where(result => !result.IsAllowed).Select(result => result.Scope.EventStore!).ToArray();

        // A denied requested store makes this a denied call, not a command from a forbidden address.
        if (denied.Contains((EventStoreName)eventStore)) return new([], [], listingFailed, RequestedDenied: true);
        return new(targets.Except(denied).ToArray(), denied, listingFailed);
    }

    static bool IsSystem(EventStoreName eventStore) => eventStore.Value.Equals(EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase);

    ConnectionCaller Caller(CallContext context)
    {
        var caller = ConnectionCallers.From(context, options.Value);
        if (context.ServerCallContext is null) return ConnectionCaller.InProcess;
        return caller with { Principal = caller.Principal is { } principal ? new ClaimsPrincipal(principal.Identities.Select(identity => identity.Clone())) : null };
    }

    async Task<(EventStoreName[] Targets, bool ListingFailed)> Targets(EventStoreName requested)
    {
        if (IsSystem(requested)) return ([requested], false);
        try
        {
            var targets = await storage.GetEventStores();
            return ([.. targets.Append(requested).Where(target => !IsSystem(target)).Distinct()], false);
        }
        catch
        {
            // Preserve the requested-store operation before reporting that cross-store coverage is incomplete.
            return ([requested], true);
        }
    }

    async Task<AuthorizationDecision> Decide(ConnectionCaller caller, Capability capability, EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var scope = AuthorizationScope.ForNamespace(eventStore, @namespace);
        if (caller.Owner == ConnectionOwner.InProcess) return AuthorizationDecision.Allow(AuthorizationReason.Granted, capability, scope);
        if (caller.Owner.IsUnidentified) return AuthorizationDecision.Deny(AuthorizationReason.NoPrincipal, capability, scope);
        try
        {
            if (caller.RequestServices is { } services)
            {
                return await services.GetRequiredService<IAuthorizationDecisions>().Decide(caller.Principal, capability, scope);
            }

            await using var serviceScope = scopes.CreateAsyncScope();
            return await serviceScope.ServiceProvider.GetRequiredService<IAuthorizationDecisions>().Decide(caller.Principal, capability, scope);
        }
        catch
        {
            return AuthorizationDecision.Deny(AuthorizationReason.Failed, capability, scope);
        }
    }

    void Log(ConnectionOwner owner, string operation, string @namespace, IEnumerable<AuthorizationDecision> results, bool listingFailed = false) =>
        logger.ComplianceDecision(
            owner.Issuer,
            owner.Subject,
            operation,
            @namespace,
            string.Join(", ", results.Select(result => $"{result.Scope.EventStore}: {result.Outcome}/{result.Reason}")),
            Enforced ? "enforce" : "audit",
            listingFailed);
}
