// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.for_QueryExecutor;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard.given;

public class a_store_capability_caller : Specification
{
    protected IAuthorizationDecisions _decisions;
    protected RecordingLoggerFactory _loggers;
    protected CallContext _context;
    protected List<(Capability Capability, AuthorizationScope Scope)> _asked;

    protected void Decide(AuthorizationReason reason, bool allowed) =>
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
        {
            _asked.Add((call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));
            return allowed
                ? AuthorizationDecision.Allow(reason, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2))
                : AuthorizationDecision.Deny(reason, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2));
        });

    void Establish()
    {
        _asked = [];
        _loggers = new();
        _decisions = Substitute.For<IAuthorizationDecisions>();
        var services = new ServiceCollection()
            .AddSingleton(_decisions)
            .AddSingleton<ILoggerFactory>(_loggers)
            .BuildServiceProvider();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "someone")], "spec")),
            RequestServices = services
        };
        _context = new(new object(), new FakeServerCallContext(http, default));
    }

    public sealed class RecordingLoggerFactory : ILoggerFactory
    {
        public List<string> Messages { get; } = [];

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => new Recorder(Messages);

        public void Dispose()
        {
        }

        sealed class Recorder(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                messages.Add(formatter(state, exception));
        }
    }
}
