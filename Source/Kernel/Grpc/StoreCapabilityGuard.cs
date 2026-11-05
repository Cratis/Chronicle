// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Decides the capability a query exercises at the event store it addresses.
/// </summary>
internal static class StoreCapabilityGuard
{
    /// <summary>
    /// Decides whether the caller of a query may exercise the capability the query declares at the event store it addresses.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="routing">The routing values the query addresses.</param>
    /// <returns>True if the query may run.</returns>
    /// <remarks>
    /// The decision is made through <see cref="IAuthorizationDecisions"/>, so a per-store entitlement takes effect here without
    /// the queries changing. Until then a caller that is not granted the capability at an ordinary event store is allowed
    /// and the decision is recorded; the System event store is refused before this is reached.
    /// </remarks>
    internal static async Task<bool> Permits(CallContext callContext, QueryRouting routing)
    {
        if (routing.Capability is null || routing.Store is null) return true;

        var httpContext = SystemStoreGuard.HttpContextFor(callContext);
        if (httpContext is null && callContext.ServerCallContext is null && SystemStoreGuard.InProcessServices is null) return true;

        var capability = new Capability(routing.Capability);
        var scope = routing.First is null
            ? AuthorizationScope.ForEventStore(routing.Store)
            : AuthorizationScope.ForNamespace(routing.Store, routing.First);

        AuthorizationDecision decision;
        ILogger logger;
        try
        {
            if (httpContext is not null)
            {
                logger = LoggerFor(httpContext.RequestServices);
                decision = await httpContext.RequestServices.GetRequiredService<IAuthorizationDecisions>().Decide(httpContext.User, capability, scope);
            }
            else if (callContext.ServerCallContext is null && SystemStoreGuard.InProcessServices is { } services)
            {
                await using var serviceScope = services.CreateAsyncScope();
                logger = LoggerFor(serviceScope.ServiceProvider);
                decision = await serviceScope.ServiceProvider.GetRequiredService<IAuthorizationDecisions>().Decide(null, capability, scope);
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        if (!decision.IsAllowed) logger.StoreCapabilityNotGranted(routing.Capability, decision.Reason.ToString());
        return Interim(decision);
    }

    /// <summary>
    /// The interim outcome for a decision: ordinary event stores stay open to authenticated callers until per-store entitlements exist.
    /// </summary>
    /// <param name="decision">The decision.</param>
    /// <returns>True if the query may run.</returns>
    static bool Interim(AuthorizationDecision decision) => decision.IsAllowed || decision.Reason != AuthorizationReason.Failed;

    static ILogger LoggerFor(IServiceProvider services) => services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(StoreCapabilityGuard).FullName!);
}
