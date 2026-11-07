// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
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
        return Decide(callContext, routing.Store, routing.Namespace, capability, StoreAccess.None);
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
        Decide(callContext, store, @namespace, null, access);

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

        var time = services.GetService<TimeProvider>() ?? TimeProvider.System;
        var expiry = cache.Changes.StartWith(Unit.Default).Select(_ => LeaseExpiry(cache, time).Repeat()).Switch();
        var revoked = cache.Changes.Merge(expiry)
            .SelectMany(_ => Observable.FromAsync(() => Permits(callContext, store, @namespace, access)))
            .Where(permitted => !permitted)
            .SelectMany(_ => Observable.Throw<Unit>(SystemStoreGuard.RefusedException()));
        return stream.TakeUntil(revoked);
    }

    /// <summary>Ends an observable query when its effective storage scope is refused.</summary>
    /// <typeparam name="T">The query result type.</typeparam>
    /// <param name="source">The query stream.</param>
    /// <param name="context">The call context.</param>
    /// <param name="routing">The storage routing.</param>
    /// <returns>The guarded stream.</returns>
    internal static IObservable<T> EndWhenRevoked<T>(IObservable<T> source, CallContext context, QueryRouting routing)
    {
        var capability = routing.Capability ?? WellKnownCapabilities.ReadEventStore.Value;
        var access = WellKnownCapabilities.AccessOf(new(capability));
        if (routing.Store is null || routing.TargetsSystem) return source;
        if (access == StoreAccess.None) return source;
        return EndWhenRevoked(source, context, routing.Store, routing.Namespace, access);
    }

    static IObservable<Unit> LeaseExpiry(IStoreEntitlementsCache cache, TimeProvider time) => Observable.Create<Unit>(observer =>
    {
        var due = cache.LoadedAt is { } loaded ? loaded + TimeSpan.FromMinutes(5) + TimeSpan.FromMilliseconds(1) - time.GetUtcNow() : TimeSpan.Zero;
        return time.CreateTimer(
            _ =>
            {
                if (cache.IsFresh) observer.OnCompleted();
                else observer.OnNext(Unit.Default);
            },
            null,
            due > TimeSpan.Zero ? due : TimeSpan.Zero,
            Timeout.InfiniteTimeSpan);
    });

    static Task<bool> Decide(CallContext callContext, string store, string? @namespace, string? capability, StoreAccess access)
    {
        var httpContext = SystemStoreGuard.HttpContextFor(callContext);
        if (httpContext is not null)
        {
            return capability is null
                ? StoreCapabilityDecision.PermitsAccess(httpContext.RequestServices, httpContext.User, access, store, @namespace)
                : StoreCapabilityDecision.Permits(httpContext.RequestServices, httpContext.User, capability, store, @namespace);
        }

        if (callContext.ServerCallContext is not null) return Task.FromResult(false);
        return SystemStoreGuard.InProcessServices is { } services
            ? DecideWithoutRequest(services, store, @namespace, capability, access)
            : Task.FromResult(true);
    }

    static async Task<bool> DecideWithoutRequest(IServiceProvider services, string store, string? @namespace, string? capability, StoreAccess access)
    {
        await using var scope = services.CreateAsyncScope();
        if (scope.ServiceProvider.GetService<InProcessKernelHost>() is not null) return true;
        return capability is null
            ? await StoreCapabilityDecision.PermitsAccess(scope.ServiceProvider, null, access, store, @namespace)
            : await StoreCapabilityDecision.Permits(scope.ServiceProvider, null, capability, store, @namespace);
    }
}
