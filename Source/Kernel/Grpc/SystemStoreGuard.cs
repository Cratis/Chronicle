// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Security;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Applies the System event store access rule to gRPC calls.
/// </summary>
internal static class SystemStoreGuard
{
    /// <summary>
    /// Gets or sets the services of the in-process server, initialized when its service facade is created.
    /// </summary>
    /// <remarks>
    /// The in-process facade assumes a single host per process. Resolving another host's facade replaces
    /// this process-wide provider; it must not be shared by hosts with different authentication configurations.
    /// </remarks>
    internal static IServiceProvider? InProcessServices { get; set; }

    /// <summary>
    /// Checks whether the caller of a call may access the System event store.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <returns>True if the caller is an administrator; false otherwise, including when there is no caller identity.</returns>
    internal static async Task<bool> IsCallerAllowed(CallContext callContext)
    {
        var httpContext = HttpContextFor(callContext);
        if (httpContext is not null)
        {
            var access = httpContext.RequestServices.GetRequiredService<ISystemStoreAccess>();
            return await access.IsAllowed(httpContext.User);
        }

        // A server call without its HTTP request remains fail-closed. Only direct in-process calls
        // consult the server's services, with no identity (allowed only while authentication is disabled).
        if (callContext.ServerCallContext is not null || InProcessServices is not { } services) return false;
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISystemStoreAccess>().IsAllowed(null);
    }

    /// <summary>
    /// Ensures that the routing values of a call are well formed and that its caller may reach the System event store when it addresses it.
    /// Calls that decide the entitlement of every store they touch themselves use this form.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="routing">The routing values the call addresses.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="RpcException">Thrown when a value contains the separator, or when the call addresses the System event store and its caller is not an administrator.</exception>
    internal static async Task Ensure(CallContext callContext, QueryRouting routing)
    {
        if (routing.HasSeparator) throw SeparatorRefused();
        if (routing.TargetsSystem && !await IsCallerAllowed(callContext)) throw Refused();
    }

    /// <summary>
    /// Ensures that the routing values of a call are well formed and that its caller may reach the event store it addresses.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="routing">The routing values the call addresses.</param>
    /// <param name="access">The access the call needs at an ordinary event store.</param>
    /// <param name="namespaceAddressed">Whether the first further routing value is the namespace the call addresses.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="RpcException">Thrown when a value contains the separator, when the call addresses the System event store and its caller is not an administrator, or when entitlements are enforced and the caller lacks the access at the event store.</exception>
    internal static async Task Ensure(CallContext callContext, QueryRouting routing, StoreAccess access, bool namespaceAddressed = true)
    {
        if (routing.HasSeparator) throw SeparatorRefused();
        if (routing.TargetsSystem)
        {
            if (!await IsCallerAllowed(callContext)) throw Refused();
            return;
        }

        if (routing.Store is null) return;
        if (!await StoreCapabilityGuard.Permits(callContext, routing.Store, namespaceAddressed ? routing.Namespace : null, access)) throw Refused();
    }

    /// <summary>Authorizes a contract without a call-context parameter from the current server request.</summary>
    /// <param name="request">The current HTTP request.</param>
    /// <param name="routing">The storage routing.</param>
    /// <param name="access">The required access.</param>
    /// <returns>The authorization task.</returns>
    /// <exception cref="RpcException">Thrown when the request is refused.</exception>
    internal static async Task EnsureNotSystemFromRequest(HttpContext? request, QueryRouting routing, StoreAccess access)
    {
        EnsureNotSystem(routing);
        if (request is null)
        {
            await EnsureNotSystem(default, routing, access);
            return;
        }

        if (routing.Store is not null && !await StoreCapabilityDecision.PermitsAccess(request.RequestServices, request.User, access, routing.Store, null)) throw Refused();
    }

    /// <summary>
    /// Ensures that a registration does not address the System event store and that its caller holds the access at the event store it addresses.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="routing">The routing values the registration addresses.</param>
    /// <param name="access">The access the registration needs.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="RpcException">Thrown when the registration is refused.</exception>
    internal static async Task EnsureNotSystem(CallContext callContext, QueryRouting routing, StoreAccess access)
    {
        EnsureNotSystem(routing);
        if (routing.Store is not null && !await StoreCapabilityGuard.Permits(callContext, routing.Store, null, access)) throw Refused();
    }

    /// <summary>
    /// Ensures that the routing values of a stream are well formed and that its caller may reach the event store it addresses,
    /// erroring the stream otherwise.
    /// </summary>
    /// <typeparam name="T">The type of the items in the stream.</typeparam>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="routing">The routing values the call addresses.</param>
    /// <param name="access">The access the stream needs at an ordinary event store.</param>
    /// <param name="source">Creates the stream once the call is allowed.</param>
    /// <returns>The stream, which errors with an <see cref="RpcException"/> when the call is refused, or when enforced entitlements change and no longer cover it.</returns>
    internal static IObservable<T> Ensure<T>(CallContext callContext, QueryRouting routing, StoreAccess access, Func<IObservable<T>> source)
    {
        if (routing.Store is null && !routing.HasSeparator) return source();
        if (routing.HasSeparator || routing.TargetsSystem) return EnsureObservableForSystem(callContext, routing, access, source);

        var store = routing.Store!;
        var @namespace = routing.Namespace;
        return Observable.FromAsync(() => Ensure(callContext, routing, access))
            .SelectMany(_ => StoreCapabilityGuard.EndWhenRevoked(source(), callContext, store, @namespace, access));
    }

    /// <summary>
    /// Ensures that a registration does not address the System event store, which is not registered through the API for anyone.
    /// </summary>
    /// <param name="routing">The routing values the call addresses.</param>
    /// <exception cref="RpcException">Thrown when a value contains the separator or the call addresses the System event store.</exception>
    internal static void EnsureNotSystem(QueryRouting routing)
    {
        if (RegistrationRefusal(routing) is { } refusal) throw refusal;
    }

    /// <summary>
    /// Gets the refusal for a registration that addresses the System event store or carries the separator.
    /// </summary>
    /// <param name="routing">The routing values the registration addresses.</param>
    /// <returns>The <see cref="RpcException"/> to fail the registration with, or null when it may proceed.</returns>
    internal static RpcException? RegistrationRefusal(QueryRouting routing)
    {
        if (routing.HasSeparator) return SeparatorRefused();
        return routing.TargetsSystem ? Refused() : null;
    }

    /// <summary>
    /// Gets the request of a call. The ASP.NET Core gRPC host keeps it in the call user state under the key its own
    /// GetHttpContext extension reads, which this project does not reference.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <returns>The <see cref="HttpContext"/>, or null when the call is not served over HTTP.</returns>
    internal static HttpContext? HttpContextFor(CallContext callContext) =>
        callContext.ServerCallContext?.UserState.TryGetValue("__HttpContext", out var value) == true ? value as HttpContext : null;

    /// <summary>
    /// Gets the refusal of a call that the caller is not allowed to make.
    /// </summary>
    /// <returns>The <see cref="RpcException"/> to fail the call with.</returns>
    internal static RpcException RefusedException() => Refused();

    /// <summary>
    /// Completes the registration of a stream once its caller is known to hold the access it needs at the event store it addresses.
    /// </summary>
    /// <typeparam name="T">The type of the registration.</typeparam>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="store">The event store the registration addresses.</param>
    /// <param name="namespace">The namespace the registration addresses.</param>
    /// <param name="access">The access the registration needs.</param>
    /// <param name="completion">The completion source that waits for the registration.</param>
    /// <param name="registration">The registration.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal static async Task CompleteRegistration<T>(CallContext callContext, string store, string? @namespace, StoreAccess access, TaskCompletionSource<T> completion, T registration)
    {
        try
        {
            if (await StoreCapabilityGuard.Permits(callContext, store, @namespace, access)) completion.TrySetResult(registration);
            else completion.TrySetException(Refused());
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }

    static IObservable<T> EnsureObservableForSystem<T>(CallContext callContext, QueryRouting routing, StoreAccess access, Func<IObservable<T>> source) =>
        Observable.FromAsync(() => Ensure(callContext, routing, access)).SelectMany(_ => source());

    static RpcException Refused() => new(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));

    static RpcException SeparatorRefused() => new(new Status(StatusCode.InvalidArgument, SystemStoreAccess.SeparatorRefused));
}
