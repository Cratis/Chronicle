// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Grpc.Core;
using Grpc.Net.Client;

namespace Cratis.Chronicle.Server.for_KernelHost;

public class when_the_silo_is_held_at_the_active_stage : given.a_held_silo
{
    bool _connected;
    HttpStatusCode _live;
    HttpStatusCode _health;
    HttpStatusCode _ready;
    HttpStatusCode _exclusive;
    HttpStatusCode _afterRelease;
    Exception? _grpcError;

    async Task Because()
    {
        await StartHeld();
        using var tcp = new TcpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await tcp.ConnectAsync(IPAddress.Loopback, _mainPort, timeout.Token);
        _connected = tcp.Connected;
        using var live = await _client.GetAsync("/health/live");
        using var health = await _client.GetAsync("/health");
        using var ready = await _client.GetAsync("/health/ready");
        using var exclusive = await _client.GetAsync("/workbench");
        _live = live.StatusCode;
        _health = health.StatusCode;
        _ready = ready.StatusCode;
        _exclusive = exclusive.StatusCode;
        using var channel = GrpcChannel.ForAddress($"http://127.0.0.1:{_mainPort}");
        var marshaller = Marshallers.Create<byte[]>(bytes => bytes, bytes => bytes);
        var method = new Method<byte[], byte[]>(MethodType.Unary, "test", "call", marshaller, marshaller);
        using var call = channel.CreateCallInvoker().AsyncUnaryCall(method, null, new CallOptions(cancellationToken: timeout.Token), []);
        _grpcError = await Catch.Exception(async () => await call.ResponseAsync);
        _hold.Release.TrySetResult();
        await _start.WaitAsync(TimeSpan.FromSeconds(10));
        using var afterRelease = await _client.GetAsync("/health");
        _afterRelease = afterRelease.StatusCode;
    }

    [Fact] void should_bind_the_main_listener_while_active_is_held() => _connected.ShouldBeTrue();
    [Fact] void should_be_live() => _live.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_not_report_legacy_health_ready() => _health.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_not_report_ready() => _ready.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_keep_the_health_port_exclusive() => _exclusive.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_return_grpc_unavailable() => ((RpcException)_grpcError!).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_report_ready_after_the_silo_starts() => _afterRelease.ShouldEqual(HttpStatusCode.OK);
}
