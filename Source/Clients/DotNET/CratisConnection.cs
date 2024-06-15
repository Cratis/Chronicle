// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Connections;
using Cratis.Kernel.Contracts.Clients;
using Cratis.Kernel.Contracts.Events;
using Cratis.Kernel.Contracts.EventSequences;
using Cratis.Kernel.Contracts.Observation;
using Cratis.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Client;

namespace Cratis;

/// <summary>
/// Represents an implementation of <see cref="ICratisConnection"/>.
/// </summary>
public class CratisConnection : ICratisConnection
{
    readonly CratisOptions _options;
    readonly ITasks _tasks;
    readonly CancellationToken _cancellationToken;
    readonly ILogger<CratisConnection> _logger;
    GrpcChannel? _channel;
    IConnectionService? _connectionService;
    DateTimeOffset _lastKeepAlive = DateTimeOffset.MinValue;
    IServices _services;
    IDisposable? _keepAliveSubscription;
    TaskCompletionSource? _connectTcs;

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisConnection"/> class.
    /// </summary>
    /// <param name="options"><see cref="CratisOptions"/>.</param>
    /// <param name="connectionLifecycle"><see cref="IConnectionLifecycle"/> for when connection state changes.</param>
    /// <param name="tasks"><see cref="ITasks"/> to create tasks with.</param>
    /// <param name="logger">Logger for logging.</param>
    /// <param name="cancellationToken">The clients <see cref="CancellationToken"/>.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public CratisConnection(
        CratisOptions options,
        IConnectionLifecycle connectionLifecycle,
        ITasks tasks,
        ILogger<CratisConnection> logger,
        CancellationToken cancellationToken)
    {
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        _options = options;
        Lifecycle = connectionLifecycle;
        _tasks = tasks;
        _cancellationToken = cancellationToken;
        _logger = logger;
    }
#pragma warning restore CS8618

    /// <inheritdoc/>
    public IConnectionLifecycle Lifecycle { get; }

    /// <inheritdoc/>
    public IServices Services
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
        _connectionService = _channel.CreateGrpcService<IConnectionService>();

        _lastKeepAlive = DateTimeOffset.UtcNow;

        _connectTcs = new TaskCompletionSource();

        _keepAliveSubscription = _connectionService.Connect(new()
        {
            ConnectionId = Lifecycle.ConnectionId,
            IsRunningWithDebugger = Debugger.IsAttached,
        }).Subscribe(HandleConnection);

        try
        {
            _services = new Services(
                _channel.CreateGrpcService<IEventSequences>(),
                _channel.CreateGrpcService<IEventTypes>(),
                _channel.CreateGrpcService<IObservers>(),
                _channel.CreateGrpcService<IClientObservers>());

            await _connectTcs.Task.WaitAsync(TimeSpan.FromSeconds(_options.ConnectTimeout));
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

        return GrpcChannel.ForAddress(
            "http://localhost:35000",
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
        _connectionService?.ConnectionKeepAlive(keepAlive).GetAwaiter().GetResult();
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
