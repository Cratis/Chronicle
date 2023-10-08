// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Kernel.Contracts.Clients;
using Aksio.Cratis.Kernel.Contracts.Events;
using Aksio.Cratis.Kernel.Contracts.EventSequences;
using Aksio.Cratis.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Client;

namespace Aksio.Cratis;

/// <summary>
/// Represents an implementation of <see cref="ICratisConnection"/>.
/// </summary>
public class CratisConnection : ICratisConnection
{
    readonly ITasks _tasks;
    readonly CancellationToken _cancellationToken;
    readonly ILogger<CratisConnection> _logger;
    GrpcChannel? _channel;
    IConnectionService? _connectionService;
    DateTimeOffset _lastKeepAlive = DateTimeOffset.MinValue;
    IServices _services;
    IDisposable? _keepAliveSubscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisConnection"/> class.
    /// </summary>
    /// <param name="connectionLifecycle"><see cref="IConnectionLifecycle"/> for when connection state changes.</param>
    /// <param name="tasks"><see cref="ITasks"/> to create tasks with.</param>
    /// <param name="logger">Logger for logging.</param>
    /// <param name="cancellationToken">The clients <see cref="CancellationToken"/>.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public CratisConnection(
        IConnectionLifecycle connectionLifecycle,
        ITasks tasks,
        ILogger<CratisConnection> logger,
        CancellationToken cancellationToken)
    {
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
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

        var httpHandler = new SocketsHttpHandler
        {
            // ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true
        };

        _channel = GrpcChannel.ForAddress(
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
        _connectionService = _channel.CreateGrpcService<IConnectionService>();

        _lastKeepAlive = DateTimeOffset.UtcNow;
        _keepAliveSubscription = _connectionService.Connect(new()
        {
            ConnectionId = Lifecycle.ConnectionId,
            IsRunningWithDebugger = Debugger.IsAttached,
        }).Subscribe(HandleConnection);
        StartWatchDog();

        _services = new Services(
            _channel.CreateGrpcService<IEventSequences>(),
            _channel.CreateGrpcService<IEventTypes>());

        await Lifecycle.Connected();
    }

    void HandleConnection(ConnectionKeepAlive keepAlive)
    {
        Console.WriteLine("Keep alive from server");
        _lastKeepAlive = DateTimeOffset.UtcNow;
        _connectionService?.ConnectionKeepAlive(keepAlive);
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

                await Lifecycle.Disconnected();
                await Connect();
            },
            _cancellationToken);
    }
}
