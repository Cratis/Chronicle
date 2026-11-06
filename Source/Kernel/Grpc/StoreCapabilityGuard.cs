// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Decides the capability a query exercises at the event store it addresses.
/// </summary>
internal static class StoreCapabilityGuard
{
    /// <summary>
    /// Decides whether the caller of a query may exercise the capability the query declares at the event store it addresses.
    /// A query that addresses an event store and declares no capability needs read access.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="routing">The routing values the query addresses.</param>
    /// <returns>True if the query may run.</returns>
    /// <remarks>
    /// The decision is shared with the HTTP query pipeline through <see cref="StoreCapabilityDecision"/>, so both transports behave the same.
    /// </remarks>
    internal static Task<bool> Permits(CallContext callContext, QueryRouting routing)
    {
        if (routing.Store is null) return Task.FromResult(true);
        var capability = routing.Capability ?? WellKnownCapabilities.ReadEventStore.Value;
        var store = routing.Store;
        var @namespace = routing.First;
        return Decide(callContext, (services, user) => StoreCapabilityDecision.Permits(services, user, capability, store, @namespace));
    }

    /// <summary>
    /// Decides whether the caller of a call holds the access the call needs at the event store (and namespace) it addresses.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="store">The addressed event store.</param>
    /// <param name="namespace">The addressed namespace, if any.</param>
    /// <param name="access">The access the call needs.</param>
    /// <returns>True if the call may run.</returns>
    internal static Task<bool> Permits(CallContext callContext, string store, string? @namespace, StoreAccess access) =>
        Decide(callContext, (services, user) => StoreCapabilityDecision.PermitsAccess(services, user, access, store, @namespace));

    /// <summary>
    /// Ends a stream with a refusal when entitlements are enforced and a change of the entitlements removes the access its caller needs.
    /// </summary>
    /// <typeparam name="T">The type of the items in the stream.</typeparam>
    /// <param name="stream">The stream.</param>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="store">The addressed event store.</param>
    /// <param name="namespace">The addressed namespace, if any.</param>
    /// <param name="access">The access the stream needs.</param>
    /// <returns>The stream, which is never ended when entitlements are only audited.</returns>
    internal static IObservable<T> EndWhenRevoked<T>(IObservable<T> stream, CallContext callContext, string store, string? @namespace, StoreAccess access)
    {
        var httpServices = SystemStoreGuard.HttpContextFor(callContext)?.RequestServices;
        var services = httpServices ?? (callContext.ServerCallContext is null ? SystemStoreGuard.InProcessServices : null);
        if (services?.GetService<IOptions<ChronicleOptions>>()?.Value.Authentication.EnforceStoreEntitlements is not true) return stream;
        if (services.GetService<IStoreEntitlementsCache>() is not { } cache) return stream;

        var revoked = cache.Changes
            .SelectMany(_ => Observable.FromAsync(() => Permits(callContext, store, @namespace, access)))
            .Where(permitted => !permitted)
            .SelectMany(_ => Observable.Throw<Unit>(SystemStoreGuard.RefusedException()));
        return stream.TakeUntil(revoked);
    }

    static async Task<bool> Decide(CallContext callContext, Func<IServiceProvider, ClaimsPrincipal?, Task<bool>> decide)
    {
        var httpContext = SystemStoreGuard.HttpContextFor(callContext);
        if (httpContext is null && callContext.ServerCallContext is null && SystemStoreGuard.InProcessServices is null) return true;

        if (httpContext is not null)
        {
            return await decide(httpContext.RequestServices, httpContext.User);
        }

        if (callContext.ServerCallContext is null && SystemStoreGuard.InProcessServices is { } services)
        {
            await using var serviceScope = services.CreateAsyncScope();

            // A direct in-process call has no caller of its own; it comes from the host that marked itself as such.
            if (serviceScope.ServiceProvider.GetService<InProcessKernelHost>() is not null) return true;
            return await decide(serviceScope.ServiceProvider, null);
        }

        return false;
    }
}
