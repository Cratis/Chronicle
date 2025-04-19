// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an interceptor that gets the correlation ID to the metadata of gRPC calls and puts it in the call context.
/// </summary>
public class CorrelationIdServerInterceptor : Interceptor
{
    /// <inheritdoc/>
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        SetCorrelationId(context);
        return base.UnaryServerHandler(request, context, continuation);
    }

    /// <inheritdoc/>
    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        SetCorrelationId(context);
        return base.ClientStreamingServerHandler(requestStream, context, continuation);
    }

    /// <inheritdoc/>
    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        SetCorrelationId(context);
        return base.ServerStreamingServerHandler(request, responseStream, context, continuation);
    }

    /// <inheritdoc/>
    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        SetCorrelationId(context);
        return base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
    }

    void SetCorrelationId(ServerCallContext context)
    {
        var metaData = context.RequestHeaders.Get(CorrelationIdClientInterceptor.CorrelationIdMetadataKey);
        if (metaData is not null && Guid.TryParse(metaData.Value, out var correlationId))
        {
            CorrelationIdAccessor.SetCurrent(correlationId);
        }
    }
}
