// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents an <see cref="Interceptor"/> that ends calls refused by the resource name checks with a precise status code.
/// </summary>
/// <remarks>
/// A rule violation is InvalidArgument, a conflicting name AlreadyExists, a quarantined name FailedPrecondition and
/// names not checked yet Unavailable. The status detail is the fixed message and never the supplied name.
/// </remarks>
internal sealed class ResourceNameExceptionInterceptor : Interceptor
{
    /// <inheritdoc/>
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception exception) when (ToStatus(exception) is { } status)
        {
            throw new RpcException(status);
        }
    }

    /// <inheritdoc/>
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (Exception exception) when (ToStatus(exception) is { } status)
        {
            throw new RpcException(status);
        }
    }

    /// <inheritdoc/>
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context);
        }
        catch (Exception exception) when (ToStatus(exception) is { } status)
        {
            throw new RpcException(status);
        }
    }

    /// <inheritdoc/>
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch (Exception exception) when (ToStatus(exception) is { } status)
        {
            throw new RpcException(status);
        }
    }

    /// <summary>
    /// Map an exception, or one it wraps, onto a status.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The <see cref="Status"/>, or null when the exception is not about resource names.</returns>
    internal static Status? ToStatus(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case InvalidResourceName invalid:
                    return new Status(StatusCode.InvalidArgument, invalid.Message);
                case ResourceNameConflict conflict:
                    return new Status(StatusCode.AlreadyExists, conflict.Message);
                case ResourceQuarantined quarantined:
                    return new Status(StatusCode.FailedPrecondition, quarantined.Message);
                case ResourceNamesNotReady notReady:
                    return new Status(StatusCode.Unavailable, notReady.Message);
            }
        }

        return null;
    }
}
