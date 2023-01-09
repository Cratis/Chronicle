// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    WebsocketClient? _webSocketClient;
    Timer? _timer;

    public WebSocketConnection(
        Uri endpoint,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<WebSocketConnection> logger)
    {
        var scheme = endpoint.Scheme == "http" ? "ws" : "wss";
        var endpointAsString = endpoint.ToString();
        endpointAsString = endpointAsString.Replace(endpoint.Scheme, scheme);
        _url = new Uri(new Uri(endpointAsString), "/api/clients");
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Connect()
    {
        _logger.Connecting(_url);
        _webSocketClient = new WebsocketClient(_url)
        {
            ReconnectTimeout = TimeSpan.FromSeconds(10)
        };
        _webSocketClient.ReconnectionHappened.Subscribe(info =>
        {
        });
        _webSocketClient.DisconnectionHappened.Subscribe(info =>
        {
        });
        _webSocketClient.MessageReceived.Subscribe(_ =>
        {
        });
        _webSocketClient.Start();

        var info = new ClientInformation(ExecutionContextManager.GlobalMicroserviceId, ConnectionId.New(), new(6, 0, 0, string.Empty));
        var serialized = JsonSerializer.Serialize(info, _jsonSerializerOptions);
        _webSocketClient.Send(serialized);

        _timer = new Timer(
            callback: _ => _webSocketClient.Send("ping"),
            state: null,
            dueTime: 0,
            period: 1000);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _webSocketClient?.Dispose();
        _timer?.Dispose();
    }
}
