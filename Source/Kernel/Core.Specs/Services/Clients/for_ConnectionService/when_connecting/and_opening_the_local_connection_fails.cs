// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using KernelConnectionService = Cratis.Chronicle.Services.Clients.ConnectionService;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_opening_the_local_connection_fails : Specification
{
    readonly given.ConnectionLogger _logger = new();
    readonly ConcurrentQueue<Exception> _errors = new();
    readonly TaskCompletionSource _terminal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    KernelConnectionService _service;
    IConnectedClients _connectedClients;
    IDisposable _subscription;
    int _heartbeats;
    int _completed;

    void Establish()
    {
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.OnClientConnected(Arg.Any<ConnectionId>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IConnectedClients>(silo.ToParsableString(), Arg.Any<string>()).Returns(_connectedClients);
        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(silo);
        var options = Options.Create(new ChronicleOptions());

        // An unavailable local tracker fails in Opened, after registration but before any keep-alive.
        _service = new KernelConnectionService(grainFactory, localSiloDetails, new ConnectedClientsQuery(grainFactory, options), null!, _logger, options);
    }

    async Task Because()
    {
        _subscription = _service.Connect(new ConnectRequest { ConnectionId = "the-client" }).Subscribe(
            _ => Interlocked.Increment(ref _heartbeats),
            error =>
            {
                _errors.Enqueue(error);
                _terminal.TrySetResult();
            },
            () =>
            {
                Interlocked.Increment(ref _completed);
                _terminal.TrySetResult();
            });
        await _terminal.Task.WaitAsync(TimeSpan.FromSeconds(2), _service.RegistrationTimeProvider);
    }

    void Destroy() => _subscription?.Dispose();

    [Fact] void should_have_registered_before_the_failure() => _connectedClients.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IConnectedClients.OnClientConnected)).ShouldEqual(1);
    [Fact] void should_report_a_non_retryable_failure() => ((RpcException)_errors.Single()).StatusCode.ShouldEqual(StatusCode.Unknown);
    [Fact] void should_send_only_generic_detail() => ((RpcException)_errors.Single()).Status.Detail.ShouldEqual("Connection failed");
    [Fact] void should_not_attach_the_original_exception() => _errors.Single().InnerException.ShouldBeNull();
    [Fact] void should_not_attach_internal_debug_details() => ((RpcException)_errors.Single()).Status.DebugException.ShouldBeNull();
    [Fact] void should_log_the_full_exception_with_the_connection_id() => _logger.Entries.Any(_ => _.Level == LogLevel.Error && _.Message.Contains("the-client", StringComparison.Ordinal) && _.Exception is NullReferenceException).ShouldBeTrue();
    [Fact] void should_report_only_one_error() => _errors.Count.ShouldEqual(1);
    [Fact] void should_not_complete_successfully() => _completed.ShouldEqual(0);
    [Fact] void should_not_send_a_heartbeat() => _heartbeats.ShouldEqual(0);
}
