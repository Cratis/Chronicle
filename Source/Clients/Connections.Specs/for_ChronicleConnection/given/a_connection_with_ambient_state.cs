// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Server;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

public class a_connection_with_ambient_state : Specification
{
    protected readonly AsyncLocal<string> _ambient = new();
    protected ChronicleConnection _connection;
    protected IConnectionLifecycle _lifecycle;
    protected ITaskFactory _tasks;
    protected ICorrelationIdAccessor _correlationIdAccessor;
    protected TaskCompletionSource<string> _keepAliveAmbient;
    protected TaskCompletionSource<Activity> _keepAliveActivity;
    protected TaskCompletionSource<string> _watchdogAmbient;
    protected TaskCompletionSource<Activity> _watchdogActivity;
    protected Activity _activity;
    protected bool _isConnected;
    WebApplication _server;
    CancellationTokenSource _cancellation;
    Task _watchdog;
    int _correlationReads;

    async Task Establish()
    {
        _cancellation = new CancellationTokenSource();
        ResetKeepAliveObservations();
        _watchdogAmbient = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _watchdogActivity = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _watchdog = Task.CompletedTask;
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _lifecycle.ConnectionId.Returns(ConnectionId.New());
        _lifecycle.IsConnected.Returns(_ => _isConnected);
        _lifecycle.Connected().Returns(_ =>
        {
            _isConnected = true;
            return Task.CompletedTask;
        });
        _tasks = Substitute.For<ITaskFactory>();
        _tasks.Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _watchdog = Task.Run(call.Arg<Func<Task>>());
            return _watchdog;
        });
        _tasks.Delay(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _watchdogAmbient.TrySetResult(_ambient.Value);
            _watchdogActivity.TrySetResult(Activity.Current);

            // The monitor stays parked until shutdown, without waiting on the clock.
            return Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
        });
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        _correlationIdAccessor.Current.Returns(_ =>
        {
            // The first read is Connect; the next is HandleConnection acknowledging the heartbeat.
            if (Interlocked.Increment(ref _correlationReads) == 2)
            {
                _keepAliveAmbient.TrySetResult(_ambient.Value);
                _keepAliveActivity.TrySetResult(Activity.Current);
            }
            return CorrelationId.New();
        });

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, endpoint => endpoint.Protocols = HttpProtocols.Http2));
        builder.Services.AddCodeFirstGrpc();
        _server = builder.Build();
        _server.MapGrpcService<keep_alive_connection>();
        await _server.StartAsync();
        _connection = new ChronicleConnection(
            new ChronicleConnectionString($"chronicle://{new Uri(_server.Urls.Single()).Authority}/?disableTls=true"),
            5,
            null,
            null,
            _lifecycle,
            _tasks,
            _correlationIdAccessor,
            NullLoggerFactory.Instance,
            _cancellation.Token,
            NullLogger<ChronicleConnection>.Instance,
            skipTlsValidation: true,
            skipCompatibilityCheck: true);
    }

    protected void ResetKeepAliveObservations()
    {
        _correlationReads = 0;
        _keepAliveAmbient = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _keepAliveActivity = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Destroy()
    {
        _activity?.Dispose();
        await _cancellation.CancelAsync();
        await _watchdog;
        _connection?.Dispose();
        await _server.DisposeAsync();
        _cancellation.Dispose();
    }

    public class keep_alive_connection : IConnectionService
    {
        public IObservable<ConnectionKeepAlive> Connect(ConnectRequest request, CallContext context = default) =>
            Observable.Return(new ConnectionKeepAlive { ConnectionId = request.ConnectionId })
                .Concat(Observable.Never<ConnectionKeepAlive>());

        public Task ConnectionKeepAlive(ConnectionKeepAlive keepAlive, CallContext context = default) => Task.CompletedTask;
        public Task<DescriptorSetResponse> GetDescriptorSet() => throw new NotSupportedException();
        public Task<CompatibilityResponse> CheckCompatibility(CompatibilityRequest request) => throw new NotSupportedException();
        public Task<IEnumerable<ConnectedClient>> GetConnectedClients(CallContext context = default) => throw new NotSupportedException();
        public IObservable<IEnumerable<ConnectedClient>> ObserveConnectedClients(CallContext context = default) => throw new NotSupportedException();
    }
}
