// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;
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
    /// The decision is shared with the HTTP query pipeline through <see cref="StoreCapabilityDecision"/>, so both transports behave the same.
    /// </remarks>
    internal static async Task<bool> Permits(CallContext callContext, QueryRouting routing)
    {
        if (routing.Capability is null || routing.Store is null) return true;

        var httpContext = SystemStoreGuard.HttpContextFor(callContext);
        if (httpContext is null && callContext.ServerCallContext is null && SystemStoreGuard.InProcessServices is null) return true;

        if (httpContext is not null)
        {
            return await StoreCapabilityDecision.Permits(httpContext.RequestServices, httpContext.User, routing.Capability, routing.Store, routing.First);
        }

        if (callContext.ServerCallContext is null && SystemStoreGuard.InProcessServices is { } services)
        {
            await using var serviceScope = services.CreateAsyncScope();
            return await StoreCapabilityDecision.Permits(serviceScope.ServiceProvider, null, routing.Capability, routing.Store, routing.First);
        }

        return false;
    }
}
