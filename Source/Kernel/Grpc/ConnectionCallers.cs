// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Resolves the <see cref="ConnectionCaller"/> of gRPC calls.
/// </summary>
internal static class ConnectionCallers
{
    /// <summary>
    /// Resolves the caller of a call. Must be called synchronously when the call starts.
    /// </summary>
    /// <param name="context">The <see cref="CallContext"/> of the call.</param>
    /// <param name="options">The server configuration.</param>
    /// <returns>The <see cref="ConnectionCaller"/>. A server call without a request, or without a subject, is unidentified.</returns>
    internal static ConnectionCaller From(CallContext context, ChronicleOptions options)
    {
        var http = SystemStoreGuard.HttpContextFor(context);
        if (!options.Authentication.Enabled) return ConnectionCaller.For(http, false);
        if (context.ServerCallContext is null) return ConnectionCaller.InProcess;
        return ConnectionCaller.For(http, true);
    }
}
