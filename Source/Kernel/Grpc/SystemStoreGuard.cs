// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
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
    /// Gets the request of a call. The ASP.NET Core gRPC host keeps it in the call user state under the key its own
    /// GetHttpContext extension reads, which this project does not reference.
    /// </summary>
    /// <param name="callContext">The <see cref="CallContext"/> of the call.</param>
    /// <returns>The <see cref="HttpContext"/>, or null when the call is not served over HTTP.</returns>
    static HttpContext? HttpContextFor(CallContext callContext) =>
        callContext.ServerCallContext?.UserState.TryGetValue("__HttpContext", out var value) == true ? value as HttpContext : null;
}
