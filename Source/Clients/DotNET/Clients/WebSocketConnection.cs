// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Timers;
using Microsoft.Extensions.Logging;
using Websocket.Client;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IWebSocketConnection"/>.
/// </summary>
public class WebSocketConnection : IWebSocketConnection
{
    /// <summary>
    /// The timeout during connection.
    /// </summary>
    public const int ConnectTimeout = 1000;

    /// <summary>
    /// The ping interval.
    /// </summary>
    public const int PingInterval = 1000;

    readonly IClientLifecycle _clientLifecycle;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<WebSocketConnection> _logger;
    readonly string _version;
    readonly IWebsocketClient _webSocketClient;
    readonly ITimerFactory _timerFactory;
    readonly IExecutionContextManager _executionContextManager;
    readonly Uri _clientEndpoint;
    bool _isDisposing;
    ITimer? _timer;

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnection"/> class.
    /// </summary>
    /// <param name="websocketClient">The <see cref="IWebsocketClient"/> to use..</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="clientEndpoint">The endpoint the Client is on.</param>
    /// <param name="clientLifecycle"><see cref="IClientLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public WebSocketConnection(
        IWebsocketClient websocketClient,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        Uri clientEndpoint,
        IClientLifecycle clientLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<WebSocketConnection> logger)
    {
        var attribute = typeof(WebSocketConnection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        _version = attribute?.InformationalVersion ?? "1.0.0";

        _webSocketClient = websocketClient;
        _timerFactory = timerFactory;
        _executionContextManager = executionContextManager;
        _clientEndpoint = clientEndpoint;
        _clientLifecycle = clientLifecycle;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Connect()
    {
        var tcs = new TaskCompletionSource<bool>();
        var firstConnectHappened = false;
        _logger.Connecting(_webSocketClient.Url);

        _webSocketClient.ReconnectTimeout = TimeSpan.FromSeconds(5);
        _webSocketClient.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);
        _webSocketClient.ReconnectionHappened.Subscribe(info =>
        {
            _logger.Reconnected();
            if (firstConnectHappened)
            {
                SendConnect();
            }
        });
        _webSocketClient.DisconnectionHappened.Subscribe(async info =>
        {
            _logger.Disconnected();
            IsConnected = false;
            await _clientLifecycle.Disconnected();
        });
        _webSocketClient.MessageReceived.Subscribe(async _ => await MessageReceived(_, tcs));
        await _webSocketClient.Start();

        SendConnect();
        firstConnectHappened = true;

        var timeoutTimer = _timerFactory.Create(_ => tcs.SetCanceled(), ConnectTimeout, Timeout.Infinite);

        try
        {
            await tcs.Task;
        }
        catch
        {
            throw new ConnectionTimedOut();
        }
        finally
        {
            timeoutTimer.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _isDisposing = true;
        _timer?.Dispose();
        SendDisconnect();
    }

    void SendConnect()
    {
        var info = new ClientInformation(_executionContextManager.Current.MicroserviceId, ConnectionId.New(), _version, _clientEndpoint.ToString());
        var serialized = JsonSerializer.Serialize(info, _jsonSerializerOptions);
        _logger.SendingClientInformation(info.ClientVersion, info.MicroserviceId, info.ConnectionId);
        _webSocketClient?.Send(serialized);
    }

    void SendDisconnect() => _webSocketClient?.Send("disconnect");

    void SendPing()
    {
        if (!_isDisposing)
        {
            _webSocketClient?.Send("ping");
        }
    }

    async Task MessageReceived(ResponseMessage _, TaskCompletionSource<bool> connectedCompletionSource)
    {
        if (_.Text == "kernel-connected")
        {
            _logger.Connected();
            connectedCompletionSource.SetResult(true);

            IsConnected = true;
            await _clientLifecycle.Connected();

            SendPing();

            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }

            _timer = _timerFactory.Create(
                callback: _ => SendPing(),
                dueTime: 0,
                period: PingInterval);
        }
    }
}
