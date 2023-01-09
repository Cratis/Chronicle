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
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<WebSocketConnection> _logger;
    readonly string _version;
    WebsocketClient? _webSocketClient;
    Timer? _timer;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnection"/> class.
    /// </summary>
    /// <param name="endpoint">The endpoint the Kernel is on.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public WebSocketConnection(
        Uri endpoint,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<WebSocketConnection> logger)
    {
        var attribute = typeof(WebSocketConnection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        _version = attribute?.InformationalVersion ?? "1.0.0";

        var scheme = endpoint.Scheme == "http" ? "ws" : "wss";
        var endpointAsString = endpoint.ToString();
        endpointAsString = endpointAsString.Replace(endpoint.Scheme, scheme);
        _url = new Uri(new Uri(endpointAsString), "/api/clients");
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Connect()
    {

        _logger.Connecting(_url);
        _webSocketClient = new WebsocketClient(_url)
        {
            ReconnectTimeout = TimeSpan.FromSeconds(5),
            ErrorReconnectTimeout = TimeSpan.FromSeconds(5)
        };
        _webSocketClient.ReconnectionHappened.Subscribe(info =>
        {
            _logger.Reconnected();
            SendConnect();
        });
        _webSocketClient.DisconnectionHappened.Subscribe(info => _logger.Disconnected());
        _webSocketClient.MessageReceived.Subscribe(_ =>
        {
            if (_.Text == "kernel-connected")
            {
                void ping() => _webSocketClient.Send("ping");
                _logger.Connected();
                ping();
                _timer = new Timer(
                    callback: _ => ping(),
                    state: null,
                    dueTime: 0,
                    period: 1000);
            }
        });
        await _webSocketClient.Start();

        SendConnect();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _webSocketClient?.Dispose();
        _timer?.Dispose();
    }

    void SendConnect()
    {
        var info = new ClientInformation(ExecutionContextManager.GlobalMicroserviceId, ConnectionId.New(), _version);
        var serialized = JsonSerializer.Serialize(info, _jsonSerializerOptions);
        _logger.SendingClientInformation(info.ClientVersion, info.MicroserviceId, info.ConnectionId);
        _webSocketClient?.Send(serialized);
    }
}
