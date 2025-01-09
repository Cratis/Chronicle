// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Client;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/>.
/// </summary>
public class ChronicleConnection : IChronicleConnection, IChronicleServicesAccessor
{
    readonly ChronicleUrl _url;
    readonly int _connectTimeout;
    readonly ITaskFactory _tasks;
    readonly CancellationToken _cancellationToken;
    readonly ILogger<ChronicleConnection> _logger;
    GrpcChannel? _channel;
    IConnectionService? _connectionService;
    DateTimeOffset _lastKeepAlive = DateTimeOffset.MinValue;
    IServices _services;
    IDisposable? _keepAliveSubscription;
    TaskCompletionSource? _connectTcs;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleConnection"/> class.
    /// </summary>
    /// <param name="url"><see cref="ChronicleUrl"/> to connect with.</param>
    /// <param name="connectTimeout">Timeout when connecting in seconds.</param>
    /// <param name="connectionLifecycle"><see cref="IConnectionLifecycle"/> for when connection state changes.</param>
    /// <param name="tasks"><see cref="ITaskFactory"/> to create tasks with.</param>
    /// <param name="logger">Logger for logging.</param>
    /// <param name="cancellationToken">The clients <see cref="CancellationToken"/>.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ChronicleConnection(
        ChronicleUrl url,
        int connectTimeout,
        IConnectionLifecycle connectionLifecycle,
        ITaskFactory tasks,
        ILogger<ChronicleConnection> logger,
        CancellationToken cancellationToken)
    {
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        _url = url;
        _connectTimeout = connectTimeout;
        Lifecycle = connectionLifecycle;
        _tasks = tasks;
        _cancellationToken = cancellationToken;
        _logger = logger;

        _cancellationToken.Register(() =>
        {
            _connectTcs?.TrySetCanceled();
            _keepAliveSubscription?.Dispose();
            _channel?.ShutdownAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
        });
    }
#pragma warning restore CS8618

    /// <inheritdoc/>
    public IConnectionLifecycle Lifecycle { get; }

    /// <inheritdoc/>
    IServices IChronicleServicesAccessor.Services
    {
        get
        {
            ConnectIfNotConnected();
            return _services;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _channel?.Dispose();
        _keepAliveSubscription?.Dispose();
    }

    void ConnectIfNotConnected()
    {
        if (!Lifecycle.IsConnected)
        {
            Connect().GetAwaiter().GetResult();
        }
    }

    async Task Connect()
    {
        _logger.Connecting();
        _channel?.Dispose();
        _keepAliveSubscription?.Dispose();

        _channel = CreateGrpcChannel();
        var clientFactory = new InProcessAwareGrpcClientProxiesClientFactory();
        _connectionService = _channel.CreateGrpcService<IConnectionService>(clientFactory);
        _lastKeepAlive = DateTimeOffset.UtcNow;
        _connectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _keepAliveSubscription = _connectionService.Connect(
            new()
            {
                ConnectionId = Lifecycle.ConnectionId,
                IsRunningWithDebugger = Debugger.IsAttached,
            }).Subscribe(HandleConnection);

        try
        {
            _services = new Services(
                _channel.CreateGrpcService<IEventStores>(clientFactory),
                _channel.CreateGrpcService<INamespaces>(clientFactory),
                _channel.CreateGrpcService<IRecommendations>(clientFactory),
                _channel.CreateGrpcService<IIdentities>(clientFactory),
                _channel.CreateGrpcService<IEventSequences>(clientFactory),
                _channel.CreateGrpcService<IEventTypes>(clientFactory),
                _channel.CreateGrpcService<IConstraints>(clientFactory),
                _channel.CreateGrpcService<IObservers>(clientFactory),
                _channel.CreateGrpcService<IFailedPartitions>(clientFactory),
                _channel.CreateGrpcService<IReactors>(clientFactory),
                _channel.CreateGrpcService<IReducers>(clientFactory),
                _channel.CreateGrpcService<IProjections>(clientFactory),
                _channel.CreateGrpcService<IJobs>(clientFactory),
                _channel.CreateGrpcService<IServer>(clientFactory));

            await _connectTcs.Task.WaitAsync(TimeSpan.FromSeconds(_connectTimeout));
            _logger.Connected();
            await Lifecycle.Connected();
        }
        catch (TimeoutException)
        {
            _logger.TimedOut();
        }
        finally
        {
            StartWatchDog();
        }
    }

    GrpcChannel CreateGrpcChannel()
    {
        var httpHandler = new SocketsHttpHandler
        {
            // ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true
        };

        var address = $"http://{_url.ServerAddress.Host}:{_url.ServerAddress.Port}";

        return GrpcChannel.ForAddress(
            address,
            new GrpcChannelOptions
            {
                HttpHandler = httpHandler,
                ServiceConfig = new ServiceConfig
                {
                    MethodConfigs =
                    {
                        new MethodConfig
                        {
                            Names = { MethodName.Default },
                            RetryPolicy = new RetryPolicy
                            {
                                MaxAttempts = 5,
                                InitialBackoff = TimeSpan.FromSeconds(1),
                                MaxBackoff = TimeSpan.FromSeconds(10),
                                BackoffMultiplier = 1.5,
                                RetryableStatusCodes = { StatusCode.Unavailable }
                            }
                        }
                    }
                }
            });
    }

    void HandleConnection(ConnectionKeepAlive keepAlive)
    {
        if (_connectTcs?.Task.IsCompleted == false)
        {
            _connectTcs?.SetResult();
        }
        _lastKeepAlive = DateTimeOffset.UtcNow;

        if (!Debugger.IsAttached)
        {
            _connectionService?.ConnectionKeepAlive(keepAlive).GetAwaiter().GetResult();
        }
    }

    void StartWatchDog()
    {
        _ = _tasks.Run(
            async () =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    await _tasks.Delay(1000, _cancellationToken);
                    var delta = DateTimeOffset.UtcNow.Subtract(_lastKeepAlive);
                    if (delta.TotalSeconds > 5)
                    {
                        break;
                    }
                }

                if (_connectTcs?.Task.IsCompleted == true)
                {
                    _logger.Disconnected();
                    await Lifecycle.Disconnected();
                }
                _logger.Reconnecting();
                await Connect();
            },
            _cancellationToken);
    }
}
