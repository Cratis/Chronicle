// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Services;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents an <see cref="Interceptor"/> that ends calls with a request stream cleanly.
/// </summary>
/// <remarks>
/// It stops reading the request stream before the call ends - see <see cref="StoppableRequestStreamReader{T}"/> for why -
/// and fails a call that would end successfully although the service recorded a failure for it - see
/// <see cref="StreamingCallFailures"/> for why.
/// </remarks>
internal sealed class StreamingCallInterceptor : Interceptor
{
    /// <inheritdoc/>
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        using var reader = new StoppableRequestStreamReader<TRequest>(requestStream);
        TResponse response;
        try
        {
            response = await continuation(reader, context);
        }
        finally
        {
            await reader.Stop();
        }

        ThrowRecordedFailure(context);
        return response;
    }

    /// <inheritdoc/>
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        using var reader = new StoppableRequestStreamReader<TRequest>(requestStream);
        try
        {
            await continuation(reader, responseStream, context);
        }
        finally
        {
            await reader.Stop();
        }

        ThrowRecordedFailure(context);
    }

    static void ThrowRecordedFailure(ServerCallContext context)
    {
        if (context.GetRecordedFailure() is { } failure)
        {
            ExceptionDispatchInfo.Throw(failure);
        }
    }
}
