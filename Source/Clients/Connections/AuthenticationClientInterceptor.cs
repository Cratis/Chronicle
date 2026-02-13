// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an interceptor that adds authentication headers to gRPC calls.
/// </summary>
/// <param name="tokenProvider">The token provider for obtaining access tokens.</param>
/// <param name="logger">Logger for logging.</param>
public class AuthenticationClientInterceptor(ITokenProvider tokenProvider, ILogger<AuthenticationClientInterceptor> logger) : Interceptor
{
    /// <summary>
    /// The metadata key for the authorization header.
    /// </summary>
    public const string AuthorizationMetadataKey = "authorization";

    /// <inheritdoc/>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var contextWithAuth = AddAuthorization(context);
        var call = continuation(request, contextWithAuth);

        return new AsyncUnaryCall<TResponse>(
            HandleResponseAsync(call.ResponseAsync, request, contextWithAuth, continuation),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    /// <inheritdoc/>
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(AddAuthorization(context));

    /// <inheritdoc/>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(request, AddAuthorization(context));

    /// <inheritdoc/>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) =>
        continuation(AddAuthorization(context));

    /// <inheritdoc/>
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation) =>
        continuation(request, AddAuthorization(context));

    async Task<TResponse> HandleResponseAsync<TRequest, TResponse>(
        Task<TResponse> responseTask,
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            return await responseTask;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            logger.AuthenticationFailedRetryingWithTokenRefresh(context.Method.FullName);

            try
            {
                await tokenProvider.Refresh();

                var contextWithRefreshedAuth = AddAuthorization(context);
                var retryCall = continuation(request, contextWithRefreshedAuth);

                logger.RetryingCallAfterTokenRefresh(context.Method.FullName);
                return await retryCall.ResponseAsync;
            }
            catch (Exception retryEx)
            {
                logger.RetryAfterTokenRefreshFailed(context.Method.FullName, retryEx);
                throw;
            }
        }
    }

    ClientInterceptorContext<TRequest, TResponse> AddAuthorization<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            var token = tokenProvider.GetAccessToken().GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(token))
            {
                logger.NoTokenAvailable(context.Method.FullName);
                return context;
            }

            logger.AddingAuthenticationHeader(context.Method.FullName);

            var headers = new Metadata();
            if (context.Options.Headers is not null)
            {
                foreach (var entry in context.Options.Headers)
                {
                    headers.Add(entry);
                }
            }

            headers.Add(AuthorizationMetadataKey, $"Bearer {token}");

            return new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                context.Options.WithHeaders(headers));
        }
        catch (Exception ex)
        {
            logger.FailedToObtainToken(context.Method.FullName, ex);
            return context;
        }
    }
}
