// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.SingleWriter;
using Grpc.Core;
using Grpc.Net.Client;

namespace Cratis.Chronicle.Server.for_KernelHost;

/// <summary>
/// A replacement kernel in a start-before-stop rollout: it binds and reports ready in standby while the current
/// kernel holds the lease, refuses calls with a retryable status, and reaches the silo's Active stage (gateway,
/// rehydration, event processing) only once the lease is free.
/// </summary>
public class when_standby_is_enabled_and_another_kernel_holds_the_lease : given.a_held_silo
{
    HttpStatusCode _live;
    HttpStatusCode _standbyReady;
    string _standbyBody;
    HttpStatusCode _standbyHealth;
    Exception? _grpcError;
    bool _activeWhileInStandby;
    string _bodyWhileStarting;
    HttpStatusCode _readyAfterStart;
    string _bodyAfterStart;

    protected override bool Standby => true;

    async Task Because()
    {
        _start = _app.StartAsync(_startup.Token);
        (_standbyReady, _standbyBody) = await WaitForReadiness();
        using var live = await _client.GetAsync("/health/live");
        _live = live.StatusCode;
        using var health = await _client.GetAsync("/health");
        _standbyHealth = health.StatusCode;

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var channel = GrpcChannel.ForAddress($"http://127.0.0.1:{_mainPort}");
        var marshaller = Marshallers.Create<byte[]>(bytes => bytes, bytes => bytes);
        var method = new Method<byte[], byte[]>(MethodType.Unary, "test", "call", marshaller, marshaller);
        using var call = channel.CreateCallInvoker().AsyncUnaryCall(method, null, new CallOptions(cancellationToken: timeout.Token), []);
        _grpcError = await Catch.Exception(async () => await call.ResponseAsync);
        _activeWhileInStandby = _hold.Entered.Task.IsCompleted;

        _leaseFree = true;
        await _hold.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _bodyWhileStarting = await _client.GetStringAsync("/health/ready");

        _hold.Release.TrySetResult();
        await _start.WaitAsync(TimeSpan.FromSeconds(10));
        using var afterStart = await _client.GetAsync("/health/ready");
        _readyAfterStart = afterStart.StatusCode;
        _bodyAfterStart = await afterStart.Content.ReadAsStringAsync();
    }

    [Fact] void should_be_live() => _live.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_report_ready_while_waiting_for_the_lease() => _standbyReady.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_name_the_standby_status() => _standbyBody.ShouldEqual(SingleWriterLeaseStandby.Status);
    [Fact] void should_report_the_health_endpoint_ready_too() => _standbyHealth.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_refuse_grpc_calls_with_unavailable() => ((RpcException)_grpcError!).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_not_start_processing_without_the_lease() => _activeWhileInStandby.ShouldBeFalse();
    [Fact] void should_stay_in_standby_until_startup_completes() => _bodyWhileStarting.ShouldEqual(SingleWriterLeaseStandby.Status);
    [Fact] void should_be_ready_once_started() => _readyAfterStart.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_report_healthy_once_started() => _bodyAfterStart.ShouldEqual("Healthy");

    async Task<(HttpStatusCode Status, string Body)> WaitForReadiness()
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (true)
        {
            using var response = await _client.GetAsync("/health/ready");
            if (response.StatusCode == HttpStatusCode.OK || DateTimeOffset.UtcNow > deadline)
            {
                return (response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            await Task.Delay(50);
        }
    }
}
