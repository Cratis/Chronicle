// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cratis.Chronicle.Connections.for_AuthenticationClientInterceptor.when_async_unary_call;

public class and_authentication_fails_on_retry : given.an_authentication_client_interceptor
{
    const string Token = "initial-token";
    const string RefreshedToken = "refreshed-token";
    bool _firstCall = true;
    Exception _exception;
    TestRequest _request;
    ClientInterceptorContext<TestRequest, TestResponse> _context;
    Interceptor.AsyncUnaryCallContinuation<TestRequest, TestResponse> _continuation;
    TaskCompletionSource<TestResponse> _firstResponseSource;
    TaskCompletionSource<TestResponse> _retryResponseSource;

    void Establish()
    {
        _request = new TestRequest();
        _context = CreateContext();
        _firstResponseSource = new TaskCompletionSource<TestResponse>();
        _retryResponseSource = new TaskCompletionSource<TestResponse>();

        _tokenProvider.GetAccessToken(Arg.Any<CancellationToken>()).Returns(Token);
        _tokenProvider.Refresh(Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(RefreshedToken));

        _continuation = (req, ctx) =>
        {
            if (_firstCall)
            {
                _firstCall = false;
                _firstResponseSource.SetException(new RpcException(new Status(StatusCode.Unauthenticated, "Token expired")));
                return CreateAsyncUnaryCall(_firstResponseSource.Task);
            }

            _retryResponseSource.SetException(new RpcException(new Status(StatusCode.Unauthenticated, "Still unauthenticated")));
            return CreateAsyncUnaryCall(_retryResponseSource.Task);
        };
    }

    async Task Because()
    {
        var result = _interceptor.AsyncUnaryCall(_request, _context, _continuation);
        _exception = await Catch.Exception(async () => await result.ResponseAsync);
    }

    [Fact] void should_call_refresh_on_token_provider() => _tokenProvider.Received(1).Refresh(Arg.Any<CancellationToken>());
    [Fact] void should_throw_exception() => _exception.ShouldNotBeNull();

    ClientInterceptorContext<TestRequest, TestResponse> CreateContext()
    {
        var method = new Method<TestRequest, TestResponse>(
            MethodType.Unary,
            "TestService",
            "TestMethod",
            Marshallers.Create(_ => [], _ => new TestRequest()),
            Marshallers.Create(_ => [], _ => new TestResponse()));

        return new ClientInterceptorContext<TestRequest, TestResponse>(
            method,
            "localhost",
            default);
    }

    AsyncUnaryCall<TestResponse> CreateAsyncUnaryCall(Task<TestResponse> responseTask) =>
        new(
            responseTask,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

    record TestRequest;
    record TestResponse
    {
        public string Message { get; init; } = string.Empty;
    }
}
