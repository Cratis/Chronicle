// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Cratis.Chronicle.Connections.for_AuthenticationClientInterceptor.when_async_unary_call;

public class and_the_token_transport_failure_names_the_host : Specification
{
    const string HostMarker = "host-marker-7f3a.invalid";

    CapturingLogger<AuthenticationClientInterceptor> _interceptorLog;
    CapturingLogger<OAuthTokenProvider> _providerLog;
    OAuthTokenProvider _provider;
    AuthenticationClientInterceptor _interceptor;
    ClientInterceptorContext<TestRequest, TestResponse> _context;
    Exception _exception;

    void Establish()
    {
        _interceptorLog = new CapturingLogger<AuthenticationClientInterceptor>();
        _providerLog = new CapturingLogger<OAuthTokenProvider>();
        _provider = new OAuthTokenProvider(
            new Uri("https://idp.example.com/oauth2/token"),
            "test-client",
            "test-secret",
            null,
            _providerLog,
            new HostNamingHandler(HostMarker),
            TimeProvider.System);
        _interceptor = new AuthenticationClientInterceptor(_provider, _interceptorLog);
        var method = new Method<TestRequest, TestResponse>(
            MethodType.Unary,
            "TestService",
            "TestMethod",
            Marshallers.Create(_ => [], _ => new TestRequest()),
            Marshallers.Create(_ => [], _ => new TestResponse()));
        _context = new ClientInterceptorContext<TestRequest, TestResponse>(method, "localhost", default);
    }

    async Task Because()
    {
        var result = _interceptor.AsyncUnaryCall(
            new TestRequest(),
            _context,
            (_, _) => new AsyncUnaryCall<TestResponse>(
                Task.FromException<TestResponse>(new RpcException(new Status(StatusCode.Unauthenticated, "Token expired"))),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));
        _exception = await Catch.Exception(async () => await result.ResponseAsync);
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_fail() => _exception.ShouldNotBeNull();
    [Fact] void should_not_carry_the_host_in_the_thrown_exception() => _exception.ToString().Contains(HostMarker, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_carry_the_host_in_any_inner_exception() => Chain(_exception).Any(_ => _.Message.Contains(HostMarker, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_log_the_host_from_the_interceptor() => _interceptorLog.Entries.Any(_ => _.Contains(HostMarker)).ShouldBeFalse();
    [Fact] void should_not_log_the_host_from_the_provider() => _providerLog.Entries.Any(_ => _.Contains(HostMarker)).ShouldBeFalse();
    [Fact] void should_log_something_from_the_interceptor() => _interceptorLog.Entries.ShouldNotBeEmpty();

    static IEnumerable<Exception> Chain(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }

    sealed class HostNamingHandler(string host) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException($"No such host is known. ({host}:443)", new System.Net.Sockets.SocketException(11001));
    }

    record TestRequest;
    record TestResponse;
}
