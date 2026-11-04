// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

using KernelConnectionService = Cratis.Chronicle.Services.Clients.ConnectionService;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

public class a_registration_lifetime : Specification
{
    protected static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(47);
    protected readonly TaskCompletionSource _registration = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _terminal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _heartbeat = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly CancellationTokenSource _cancellation = new();
    protected readonly RegistrationClock _clock = new();
    protected readonly ConnectionLogger _logger = new();
    protected readonly ConcurrentQueue<Exception> _errors = new();
    protected readonly ConcurrentQueue<string> _events = new();
    protected Func<Task> _register;
    protected IConnectedClients _connectedClients;
    protected ConnectionKeepAlive _firstKeepAlive;
    protected Exception _connectError;
    protected bool _terminalArrived;
    protected bool _timerCreated;
    protected bool _heartbeatArrived;
    protected bool _cleanupArrived;
    protected bool _heartbeatBeforeRegistration;
    protected int _completed;
    protected int _heartbeats;
    KernelConnectionService _service;
    IDisposable _subscription;
    readonly OpenConnections _openConnections = new();

    protected bool LocallyOpen => _openConnections.Responded("the-client", false, DateTimeOffset.UtcNow, TimeSpan.FromHours(1), out _);
    protected int Disconnects => CallsTo(nameof(IConnectedClients.OnClientDisconnected));
    protected int Registrations => CallsTo(nameof(IConnectedClients.OnClientConnected));

    void Establish()
    {
        _register = () => _registration.Task;
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.OnClientConnected(Arg.Any<ConnectionId>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(_ =>
        {
            _entered.TrySetResult();
            return _register();
        });
        _connectedClients.OnClientDisconnected(Arg.Any<ConnectionId>(), Arg.Any<string>()).Returns(_ =>
        {
            _disconnected.TrySetResult();
            return Task.CompletedTask;
        });
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IConnectedClients>(silo.ToParsableString(), Arg.Any<string>()).Returns(_connectedClients);
        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(silo);
        var configuration = new ChronicleOptions { ConnectedClients = new() { KeepAliveIntervalSeconds = 30 } };
        configuration.Clustering.ResponseTimeout = ResponseTimeout;
        var options = Options.Create(configuration);
        _service = new KernelConnectionService(
            grainFactory,
            localSiloDetails,
            new ConnectedClientsQuery(grainFactory, options),
            _openConnections,
            _logger,
            options)
        {
            RegistrationTimeProvider = _clock
        };
    }

    async Task Destroy()
    {
        await _cancellation.CancelAsync();

        // Settle every test-owned remote task, including when a red assertion fails.
        _registration.TrySetException(new InvalidOperationException("Registration fixture stopped"));

        // Observe the test-owned task; this is not evidence of production late-fault observation.
        await _registration.Task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (_heartbeats > 0)
        {
            await Arrives(_disconnected.Task);
        }

        _subscription?.Dispose();
        _cancellation.Dispose();
    }

    protected void Connect()
    {
        try
        {
            _subscription = _service.Connect(
                new ConnectRequest { ConnectionId = "the-client" },
                new CallContext(new CallOptions(cancellationToken: _cancellation.Token)))
                .Subscribe(
                    keepAlive =>
                    {
                        _firstKeepAlive ??= keepAlive;
                        _heartbeatBeforeRegistration |= !_registration.Task.IsCompletedSuccessfully;
                        Interlocked.Increment(ref _heartbeats);
                        _events.Enqueue("heartbeat");
                        _heartbeat.TrySetResult();
                    },
                    error =>
                    {
                        _errors.Enqueue(error);
                        _events.Enqueue("error");
                        _terminal.TrySetResult();
                    },
                    () =>
                    {
                        Interlocked.Increment(ref _completed);
                        _events.Enqueue("completed");
                        _terminal.TrySetResult();
                    });
        }
        catch (Exception error)
        {
            _connectError = error;
        }
    }

    protected async Task ExpireRegistration()
    {
        _timerCreated = await Arrives(_clock.Created);
        _clock.Expire();
        _terminalArrived = await Arrives(_terminal.Task);
    }

    protected static async Task<bool> Arrives(Task fact)
    {
        try
        {
            // Failure guard only: all registration/deadline orderings use explicit signals.
            await fact.WaitAsync(TimeSpan.FromSeconds(2));
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    int CallsTo(string method) => _connectedClients.ReceivedCalls().Count(call => call.GetMethodInfo().Name == method);
}
