// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Websocket.Client;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IWebSocketConnection"/>.
/// </summary>
public class WebSocketConnection : IWebSocketConnection
{
    readonly Uri _url;
    readonly Uri _clientEndpoint;
    readonly IClientLifecycle _clientLifecycle;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<WebSocketConnection> _logger;
    readonly string _version;
    WebsocketClient? _webSocketClient;
    Timer? _timer;

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnection"/> class.
    /// </summary>
    /// <param name="endpoint">The endpoint the Kernel is on.</param>
    /// <param name="clientEndpoint">The endpoint the Client is on.</param>
    /// <param name="clientLifecycle"><see cref="IClientLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public WebSocketConnection(
        Uri endpoint,
        Uri clientEndpoint,
        IClientLifecycle clientLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<WebSocketConnection> logger)
    {
        var attribute = typeof(WebSocketConnection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        _version = attribute?.InformationalVersion ?? "1.0.0";

        var scheme = endpoint.Scheme == "http" ? "ws" : "wss";
        var endpointAsString = endpoint.ToString();
        endpointAsString = endpointAsString.Replace(endpoint.Scheme, scheme);
        _url = new Uri(new Uri(endpointAsString), "/api/clients");
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
        _logger.Connecting(_url);
        _webSocketClient = new WebsocketClient(_url)
        {
            ReconnectTimeout = TimeSpan.FromSeconds(5),
            ErrorReconnectTimeout = TimeSpan.FromSeconds(5)
        };
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

        var timedOut = false;
        var timeoutTimer = new Timer(_ => timedOut = true, null, 1000, 100000);
        await tcs.Task;

        timeoutTimer.Dispose();

        if (timedOut)
        {
            throw new ConnectionTimedOut();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        SendDisconnect();
        _webSocketClient?.Dispose();
        _timer?.Dispose();
    }

    void SendConnect()
    {
        var info = new ClientInformation(ExecutionContextManager.GlobalMicroserviceId, ConnectionId.New(), _version, _clientEndpoint.ToString());
        var serialized = JsonSerializer.Serialize(info, _jsonSerializerOptions);
        _logger.SendingClientInformation(info.ClientVersion, info.MicroserviceId, info.ConnectionId);
        _webSocketClient?.Send(serialized);
    }

    void SendDisconnect() => _webSocketClient?.Send("disconnect");

    void SendPing() => _webSocketClient?.Send("ping");

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

            _timer = new Timer(
                callback: _ => SendPing(),
                state: null,
                dueTime: 0,
                period: 1000);
        }
    }
}
