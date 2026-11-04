// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_AuthenticationClientInterceptor.when_async_unary_call;

public class and_the_token_cannot_be_obtained : given.an_authentication_client_interceptor
{
    RecordingLogger<AuthenticationClientInterceptor> _recordingLogger;
    AsyncUnaryCall<TestResponse> _result;
    ClientInterceptorContext<TestRequest, TestResponse> _context;
    ClientInterceptorContext<TestRequest, TestResponse> _receivedContext;
    Interceptor.AsyncUnaryCallContinuation<TestRequest, TestResponse> _continuation;
    bool _called;

    void Establish()
    {
        _recordingLogger = new RecordingLogger<AuthenticationClientInterceptor>();
        _interceptor = new AuthenticationClientInterceptor(_tokenProvider, _recordingLogger);
        _tokenProvider.GetAccessToken(Arg.Any<CancellationToken>()).Returns(Task.FromException<string?>(new HttpRequestException("Connection refused")));
        var method = new Method<TestRequest, TestResponse>(
            MethodType.Unary,
            "TestService",
            "TestMethod",
            Marshallers.Create(_ => [], _ => new TestRequest()),
            Marshallers.Create(_ => [], _ => new TestResponse()));
        _context = new ClientInterceptorContext<TestRequest, TestResponse>(method, "localhost", default);
        _continuation = (request, context) =>
        {
            _called = true;
            _receivedContext = context;
            return new AsyncUnaryCall<TestResponse>(
                Task.FromResult(new TestResponse()),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        };
    }

    async Task Because()
    {
        _result = _interceptor.AsyncUnaryCall(new TestRequest(), _context, _continuation);
        await _result.ResponseAsync;
    }

    [Fact] void should_still_make_the_call() => _called.ShouldBeTrue();
    [Fact] void should_not_add_an_authorization_header() => (_receivedContext.Options.Headers?.Any(_ => _.Key == "authorization") ?? false).ShouldBeFalse();
    [Fact] void should_not_log_a_warning_or_error() => _recordingLogger.Entries.Where(_ => _.Level >= LogLevel.Warning).ShouldBeEmpty();
    [Fact] void should_log_at_debug() => _recordingLogger.AtLevel(LogLevel.Debug).ShouldNotBeEmpty();

    record TestRequest;
    record TestResponse;
}
