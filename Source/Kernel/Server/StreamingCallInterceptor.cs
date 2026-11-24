// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.Load;
using Cratis.Chronicle.Services;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents an <see cref="Interceptor"/> that ends streaming calls with their recorded failure.
/// </summary>
/// <remarks>
/// It stops reading the request stream before the call ends - see <see cref="StoppableRequestStreamReader{T}"/> for why -
/// and fails a call that would end successfully although the service recorded a failure for it - see
/// <see cref="StreamingCallFailures"/> for why.
/// <para>
/// It also counts the streaming calls being served, for the kernel's load report.
/// </para>
/// </remarks>
/// <param name="load">The <see cref="IKernelLoad"/> to report open streams to, if any.</param>
internal sealed class StreamingCallInterceptor(IKernelLoad? load = null) : Interceptor
{
    /// <inheritdoc/>
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        using var reader = new StoppableRequestStreamReader<TRequest>(requestStream);
        TResponse response;
        load?.StreamOpened();
        try
        {
            response = await continuation(reader, context);
        }
        finally
        {
            load?.StreamClosed();
            await reader.Stop();
        }

        context.ThrowRecordedFailure();
        return response;
    }

    /// <inheritdoc/>
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        load?.StreamOpened();
        try
        {
            await continuation(request, new GuardedResponseStreamWriter<TResponse>(responseStream, context), context);
        }
        finally
        {
            load?.StreamClosed();
        }

        context.ThrowRecordedFailure();
    }

    /// <inheritdoc/>
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        using var reader = new StoppableRequestStreamReader<TRequest>(requestStream);
        load?.StreamOpened();
        try
        {
            await continuation(reader, new GuardedResponseStreamWriter<TResponse>(responseStream, context), context);
        }
        finally
        {
            load?.StreamClosed();
            await reader.Stop();
        }

        context.ThrowRecordedFailure();
    }
}
