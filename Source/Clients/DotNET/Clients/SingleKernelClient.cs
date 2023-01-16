// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;
using Aksio.Cratis.Commands;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Queries;
using Aksio.Cratis.Timers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Websocket.Client;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClient"/> for a single instance.
/// </summary>
public class SingleKernelClient : IClient, IDisposable
{
    readonly IHttpClientFactory _clientFactory;
    readonly ITimerFactory _timerFactory;
    readonly IExecutionContextManager _executionContextManager;
    readonly SingleKernelOptions _options;
    readonly Uri _clientEndpoint;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IClientLifecycle _clientLifecycle;
    readonly ILogger<SingleKernelClient> _logger;
    readonly ILogger<WebSocketConnection> _webSocketConnectionLogger;
    private WebsocketClient? _websocketClient;
    IWebSocketConnection? _connection;

    /// <inheritdoc/>
    public bool IsConnected => _connection?.IsConnected ?? false;

    /// <inheritdoc/>
    public ConnectionId ConnectionId => _connection?.ConnectionId ?? ConnectionId.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleKernelClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">An <see cref="IHttpClientFactory"/> to create clients from.</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="options">The <see cref="SingleKernelOptions"/> to use for connecting.</param>
    /// <param name="clientEndpoint">The client endpoint.</param>
    /// <param name="clientLifecycle"><see cref="IClientLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="webSocketConnectionLogger"><see cref="ILogger"/> for passing to the <see cref="WebSocketConnection"/>.</param>
    public SingleKernelClient(
        IHttpClientFactory httpClientFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        SingleKernelOptions options,
        Uri clientEndpoint,
        IClientLifecycle clientLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<SingleKernelClient> logger,
        ILogger<WebSocketConnection> webSocketConnectionLogger)
    {
        _clientFactory = httpClientFactory;
        _timerFactory = timerFactory;
        _executionContextManager = executionContextManager;
        _options = options;
        _clientEndpoint = clientEndpoint;
        _jsonSerializerOptions = jsonSerializerOptions;
        _clientLifecycle = clientLifecycle;
        _logger = logger;
        _webSocketConnectionLogger = webSocketConnectionLogger;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection?.Dispose();
        _websocketClient?.Dispose();
    }

    /// <inheritdoc/>
    public async Task Connect()
    {
        _logger.Connecting(_options.Endpoint.ToString());

        for (; ; )
        {
            var client = _clientFactory.CreateClient();
            client.BaseAddress = _options.Endpoint;
            try
            {
                var response = await client.GetAsync("/api/clients/ping");
                break;
            }
            catch
            {
            }
            _logger.KernelUnavailable();
            await Task.Delay(2000);
        }

        var endpoint = new Uri(_options.Endpoint, "/api/clients").ToWebSocketEndpoint();
        _websocketClient = new WebsocketClient(endpoint);
        _connection = new WebSocketConnection(
            _websocketClient,
            _timerFactory,
            _executionContextManager,
            _clientEndpoint,
            _clientLifecycle,
            _jsonSerializerOptions,
            _webSocketConnectionLogger);
        await _connection.Connect();
    }

    /// <inheritdoc/>
    public async Task<CommandResult> PerformCommand(string route, object? command = null)
    {
        _logger.PerformingCommand(_options.Endpoint.ToString(), route);
        ThrowIfClientIsDisconnected();

        var client = _clientFactory.CreateClient();
        client.BaseAddress = _options.Endpoint;
        HttpResponseMessage response;

        if (command is not null)
        {
            response = await client.PostAsJsonAsync(route, command, options: _jsonSerializerOptions);
        }
        else
        {
            response = await client.PostAsync(route, null);
        }
        var result = await response.Content.ReadFromJsonAsync<CommandResult>(_jsonSerializerOptions);
        return result!;
    }

    /// <inheritdoc/>
    public async Task<QueryResult> PerformQuery(string route, IDictionary<string, string>? queryString = null)
    {
        _logger.PerformingQuery(_options.Endpoint.ToString(), route);
        ThrowIfClientIsDisconnected();

        var client = _clientFactory.CreateClient();
        client.BaseAddress = _options.Endpoint;
        HttpResponseMessage response;

        if (queryString is not null)
        {
            var uri = QueryHelpers.AddQueryString(route, queryString!);
            response = await client.GetAsync(uri);
        }
        else
        {
            response = await client.GetAsync(route);
        }
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(_jsonSerializerOptions);
        return result!;
    }

    void ThrowIfClientIsDisconnected()
    {
        if (!IsConnected)
        {
            throw new DisconnectedClient();
        }
    }
}
