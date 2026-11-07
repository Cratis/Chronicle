// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Linq;
using System.Security.Claims;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Services.Clients;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Server;

namespace Cratis.Chronicle.Server.for_ConnectionOwnership.given;

public class an_authenticated_grpc_connection : Specification
{
    protected const string Id = "connection";
    protected ConnectionOwnership _ownership;
    protected IConnectionService _client;
    WebApplication _app;
    GrpcChannel _channel;
    readonly List<IDisposable> _subscriptions = [];
    readonly CancellationTokenSource _deadline = new(TimeSpan.FromSeconds(10));

    async Task Establish()
    {
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        var connectedClients = Substitute.For<IConnectedClients>();
        connectedClients.OnClientPing(Arg.Any<ConnectionId>()).Returns(true);
        var management = Substitute.For<IManagementGrain>();
        management.GetHosts(true).Returns(new Dictionary<SiloAddress, SiloStatus> { [silo] = SiloStatus.Active });
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IConnectedClients>(silo.ToParsableString(), Arg.Any<string>()).Returns(connectedClients);
        grains.GetGrain<IManagementGrain>(Arg.Any<long>(), Arg.Any<string>()).Returns(management);
        var localSilo = Substitute.For<ILocalSiloDetails>();
        localSilo.SiloAddress.Returns(silo);
        var options = Options.Create(new ChronicleOptions { Authentication = new() { Enabled = true } });
        _ownership = new();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCodeFirstGrpc();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(grains);
        builder.Services.AddSingleton(localSilo);
        builder.Services.AddSingleton<ConnectedClientsQuery>();
        builder.Services.AddSingleton<OpenConnections>();
        builder.Services.AddSingleton<IConnectionOwnership>(_ownership);
        builder.Services.AddSingleton(Substitute.For<IConnectedClientsVisibility>());
        builder.Services.AddSingleton<ConnectionService>();
        _app = builder.Build();
        _app.Use(async (http, next) =>
        {
            // Supply a validated identity without involving a token authority or an Orleans silo.
            var subject = http.Request.Headers["test-subject"].ToString();
            if (subject.Length > 0)
            {
                http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject, ClaimValueTypes.String, "issuer")], "test"));
            }
            await next(http);
        });
        _app.MapGrpcService<ConnectionService>();
        await _app.StartAsync();
        _channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() });
        _client = _channel.CreateGrpcService<IConnectionService>();
    }

    protected async Task<Exception?> Open(string subject)
    {
        var outcome = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscriptions.Add(_client.Connect(new ConnectRequest { ConnectionId = Id }, ContextFor(subject)).Subscribe(
            _ => outcome.TrySetResult(null),
            error => outcome.TrySetResult(error)));
        return await outcome.Task.WaitAsync(_deadline.Token);
    }

    protected CallContext ContextFor(string subject) => new(new CallOptions(
        headers: new Metadata { { "test-subject", subject } },
        cancellationToken: _deadline.Token));

    async Task Destroy()
    {
        foreach (var subscription in _subscriptions) subscription.Dispose();
        await _deadline.CancelAsync();
        _channel.Dispose();
        await _app.DisposeAsync();
        _deadline.Dispose();
    }
}
