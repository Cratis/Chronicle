// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Makes transport identity available to connection operations without a call-context parameter.
/// </summary>
/// <param name="owners">The continuation-local principal resolver.</param>
public class ConnectionOwnerInterceptor(IConnectionOwnerResolver owners) : Interceptor
{
    /// <inheritdoc/>
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation) =>
        NeedsPrincipal(context.Method) ? UnaryWithPrincipal(request, context, continuation) : continuation(request, context);

    /// <inheritdoc/>
    public override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation) =>
        NeedsPrincipal(context.Method) ? ServerStreamingWithPrincipal(request, responseStream, context, continuation) : continuation(request, responseStream, context);

    /// <inheritdoc/>
    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation) =>
        NeedsPrincipal(context.Method) ? ClientStreamingWithPrincipal(requestStream, context, continuation) : continuation(requestStream, context);

    /// <inheritdoc/>
    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation) =>
        NeedsPrincipal(context.Method) ? DuplexStreamingWithPrincipal(requestStream, responseStream, context, continuation) : continuation(requestStream, responseStream, context);

    static bool NeedsPrincipal(string method) => method is
        "/Cratis.Chronicle.Contracts.Clients.ConnectionService/Connect" or
        "/Cratis.Chronicle.Contracts.Clients.ConnectionService/ConnectionKeepAlive" or
        "/Cratis.Chronicle.Contracts.Clients.ConnectionService/GetConnectedClients" or
        "/Cratis.Chronicle.Contracts.Clients.ConnectionService/ObserveConnectedClients" or
        "/Cratis.Chronicle.Contracts.Observation.Reactors.Reactors/Observe" or
        "/Cratis.Chronicle.Contracts.Observation.Reducers.Reducers/Observe" or
        "/Cratis.Chronicle.Contracts.Observation.Observers/GetConnectedClientsForObserver";

    Task<TResponse> UnaryWithPrincipal<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class => WithPrincipal(context, () => continuation(request, context));

    Task ServerStreamingWithPrincipal<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class => WithPrincipal(context, () => continuation(request, responseStream, context));

    Task<TResponse> ClientStreamingWithPrincipal<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class => WithPrincipal(context, () => continuation(requestStream, context));

    Task DuplexStreamingWithPrincipal<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class => WithPrincipal(context, () => continuation(requestStream, responseStream, context));

    async Task<TResponse> WithPrincipal<TResponse>(ServerCallContext context, Func<Task<TResponse>> continuation)
    {
        var previous = owners.Principal;
        var previousTransport = owners.HasTransport;
        owners.Principal = context.GetHttpContext().User;
        owners.HasTransport = true;
        try
        {
            return await continuation();
        }
        finally
        {
            owners.Principal = previous;
            owners.HasTransport = previousTransport;
        }
    }

    async Task WithPrincipal(ServerCallContext context, Func<Task> continuation)
    {
        var previous = owners.Principal;
        var previousTransport = owners.HasTransport;
        owners.Principal = context.GetHttpContext().User;
        owners.HasTransport = true;
        try
        {
            await continuation();
        }
        finally
        {
            owners.Principal = previous;
            owners.HasTransport = previousTransport;
        }
    }
}
