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
    /// Checks whether the caller of a call may access the System event store.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <returns>True if the caller is an administrator; false otherwise, including when there is no caller identity.</returns>
    internal static async Task<bool> IsCallerAllowed(CallContext callContext)
    {
        var httpContext = HttpContextFor(callContext);
        if (httpContext is null) return false;

        var access = httpContext.RequestServices.GetRequiredService<ISystemStoreAccess>();
        return await access.IsAllowed(httpContext.User);
    }

    /// <summary>
    /// Ensures that the routing values of a call are well formed and that its caller may reach the event store it addresses.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="routing">The routing values the call addresses.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="RpcException">Thrown when a value contains the separator, or when the call addresses the System event store and its caller is not an administrator.</exception>
    internal static async Task Ensure(CallContext callContext, QueryRouting routing)
    {
        if (routing.HasSeparator) throw SeparatorRefused();
        if (!routing.TargetsSystem) return;
        if (!await IsCallerAllowed(callContext)) throw Refused();
    }

    /// <summary>
    /// Ensures that the routing values of a stream are well formed and that its caller may reach the event store it addresses,
    /// erroring the stream otherwise.
    /// </summary>
    /// <typeparam name="T">The type of the items in the stream.</typeparam>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <param name="routing">The routing values the call addresses.</param>
    /// <param name="source">Creates the stream once the call is allowed.</param>
    /// <returns>The stream, which errors with an <see cref="RpcException"/> when the call is refused.</returns>
    internal static IObservable<T> Ensure<T>(CallContext callContext, QueryRouting routing, Func<IObservable<T>> source)
    {
        if (!routing.HasSeparator && !routing.TargetsSystem) return source();
        return Observable.FromAsync(() => Ensure(callContext, routing)).SelectMany(_ => source());
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

    static RpcException Refused() => new(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));

    static RpcException SeparatorRefused() => new(new Status(StatusCode.InvalidArgument, SystemStoreAccess.SeparatorRefused));

    /// <summary>
    /// Gets the request of a call. The ASP.NET Core gRPC host keeps it in the call user state under the key its own
    /// GetHttpContext extension reads, which this project does not reference.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <returns>The <see cref="HttpContext"/>, or null when the call is not served over HTTP.</returns>
    static HttpContext? HttpContextFor(CallContext callContext) =>
        callContext.ServerCallContext?.UserState.TryGetValue("__HttpContext", out var value) == true ? value as HttpContext : null;
}
