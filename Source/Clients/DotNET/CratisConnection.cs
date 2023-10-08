// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Kernel.Contracts.Clients;
using Aksio.Cratis.Kernel.Contracts.Events;
using Aksio.Cratis.Kernel.Contracts.EventSequences;
using Aksio.Cratis.Tasks;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;

namespace Aksio.Cratis;

/// <summary>
/// Represents an implementation of <see cref="ICratisConnection"/>.
/// </summary>
public class CratisConnection : ICratisConnection
{
    readonly ITasks _tasks;
    readonly CancellationToken _cancellationToken;
    GrpcChannel? _channel;
    IConnectionService? _connectionService;
    DateTimeOffset _lastKeepAlive = DateTimeOffset.MinValue;
    IServices _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisConnection"/> class.
    /// </summary>
    /// <param name="connectionLifecycle"><see cref="IConnectionLifecycle"/> for when connection state changes.</param>
    /// <param name="tasks"><see cref="ITasks"/> to create tasks with.</param>
    /// <param name="cancellationToken">The clients <see cref="CancellationToken"/>.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public CratisConnection(
        IConnectionLifecycle connectionLifecycle,
        ITasks tasks,
        CancellationToken cancellationToken)
    {
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        Lifecycle = connectionLifecycle;
        _tasks = tasks;
        _cancellationToken = cancellationToken;
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
        _channel?.Dispose();
        _channel = GrpcChannel.ForAddress("http://localhost:35000");
        _connectionService = _channel.CreateGrpcService<IConnectionService>();

        _lastKeepAlive = DateTimeOffset.UtcNow;
        _connectionService.Connect(new()
        {
            ConnectionId = Lifecycle.ConnectionId,
            IsRunningWithDebugger = Debugger.IsAttached,
        }).Subscribe(HandleConnection);
        HandleKeepAlive();

        _services = new Services(
            _channel.CreateGrpcService<IEventSequences>(),
            _channel.CreateGrpcService<IEventTypes>());

        await Lifecycle.Connected();
    }

    void HandleConnection(ConnectionKeepAlive keepAlive)
    {
        _lastKeepAlive = DateTimeOffset.UtcNow;
        _connectionService?.ConnectionKeepAlive(keepAlive);
    }

    void HandleKeepAlive()
    {
        _ = _tasks.Run(
            async () =>
            {
                while (_cancellationToken.IsCancellationRequested)
                {
                    await _tasks.Delay(1000, _cancellationToken);
                    var delta = DateTimeOffset.UtcNow.Subtract(_lastKeepAlive);
                    if (delta.TotalSeconds > 5)
                    {
                        await Lifecycle.Disconnected();
                        await Connect();
                    }
                }
            },
            _cancellationToken);
    }
}
