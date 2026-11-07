// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Subjects;
using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Server;

namespace Cratis.Chronicle.Server.for_GrpcCallerResolution.given;

/// <summary>
/// Hosts a gRPC service on a real ASP.NET Core pipeline and calls it over the test server's HTTP/2 handler, so
/// the call context is the host's own - nothing seeds the request into the call user state.
/// </summary>
public class a_grpc_host_with_store_entitlements : Specification
{
    protected const string EntitledSubject = "entitled";
    protected const string UnentitledSubject = "unentitled";
    protected const string Store = "shop";

    protected WebApplication _app;
    protected GrpcChannel _channel;
    protected IGuardProbe _probe;
    protected Subject<Unit> _changes;
    protected bool _revoked;

    async Task Establish()
    {
        _changes = new();
        var decisions = Substitute.For<IAuthorizationDecisions>();
        decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
        {
            var granted = !Volatile.Read(ref _revoked) && call.Arg<ClaimsPrincipal?>()?.FindFirst("sub")?.Value == EntitledSubject;
            return granted
                ? AuthorizationDecision.Allow(AuthorizationReason.Granted, call.Arg<Capability>(), call.Arg<AuthorizationScope>())
                : AuthorizationDecision.Deny(AuthorizationReason.NotGranted, call.Arg<Capability>(), call.Arg<AuthorizationScope>());
        });
        var cache = Substitute.For<IStoreEntitlementsCache>();
        cache.Changes.Returns(_changes);
        cache.IsFresh.Returns(true);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddCodeFirstGrpc();
        builder.Services.AddAuthentication(SubjectHeaderAuthentication.Scheme)
            .AddScheme<AuthenticationSchemeOptions, SubjectHeaderAuthentication>(SubjectHeaderAuthentication.Scheme, null);
        builder.Services.AddSingleton(decisions);
        builder.Services.AddSingleton(cache);
        builder.Services.AddSingleton(Options.Create(new ChronicleOptions { Authentication = new() { Enabled = true, EnforceStoreEntitlements = true } }));
        _app = builder.Build();
        _app.UseAuthentication();
        _app.MapGrpcService<GuardProbe>();
        await _app.StartAsync();

        var server = _app.GetTestServer();
        _channel = GrpcChannel.ForAddress(server.BaseAddress, new GrpcChannelOptions { HttpHandler = server.CreateHandler() });
        _probe = _channel.CreateGrpcService<IGuardProbe>();
    }

    async Task Destroy()
    {
        _channel.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    protected static CallContext As(string? subject)
    {
        var headers = new global::Grpc.Core.Metadata();
        if (subject is not null) headers.Add(SubjectHeaderAuthentication.Header, subject);
        return new CallContext(new global::Grpc.Core.CallOptions(headers));
    }
}
