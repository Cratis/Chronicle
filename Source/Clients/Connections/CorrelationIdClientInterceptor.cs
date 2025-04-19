// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an interceptor that adds a correlation ID to the metadata of gRPC calls.
/// </summary>
/// <param name="correlationIdAccessor">The accessor for the correlation ID.</param>
public class CorrelationIdClientInterceptor(ICorrelationIdAccessor correlationIdAccessor) : Interceptor
{
    /// <summary>
    /// The key used to store the correlation ID in the metadata.
    /// </summary>
    public const string CorrelationIdMetadataKey = "x-correlation-id";

    /// <inheritdoc/>
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        context = AddCorrelationId(context);
        return base.BlockingUnaryCall(request, context, continuation);
    }

    /// <inheritdoc/>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        context = AddCorrelationId(context);
        return base.AsyncUnaryCall(request, context, continuation);
    }

    /// <inheritdoc/>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        context = AddCorrelationId(context);
        return base.AsyncServerStreamingCall(request, context, continuation);
    }

    /// <inheritdoc/>
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        context = AddCorrelationId(context);
        return base.AsyncClientStreamingCall(context, continuation);
    }

    /// <inheritdoc/>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        context = AddCorrelationId(context);
        return base.AsyncDuplexStreamingCall(context, continuation);
    }

    ClientInterceptorContext<TRequest, TResponse> AddCorrelationId<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        var newOptions = context.Options.WithHeaders(new Metadata
        {
            { CorrelationIdMetadataKey, correlationIdAccessor.Current.ToString() }
        });
        return new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            newOptions);
    }
}
