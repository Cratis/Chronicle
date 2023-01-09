// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;
using Aksio.Cratis.Commands;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Queries;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClient"/> for a single instance.
/// </summary>
public class SingleKernelClient : IClient, IDisposable
{
    readonly IHttpClientFactory _clientFactory;
    readonly SingleKernelOptions _options;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<SingleKernelClient> _logger;
    readonly ILogger<WebSocketConnection> _webSocketConnectionLogger;
    IWebSocketConnection? _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleKernelClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">An <see cref="IHttpClientFactory"/> to create clients from.</param>
    /// <param name="options">The <see cref="SingleKernelOptions"/> to use for connecting.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="webSocketConnectionLogger"><see cref="ILogger"/> for passing to the <see cref="WebSocketConnection"/>.</param>
    public SingleKernelClient(
        IHttpClientFactory httpClientFactory,
        SingleKernelOptions options,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<SingleKernelClient> logger,
        ILogger<WebSocketConnection> webSocketConnectionLogger)
    {
        _clientFactory = httpClientFactory;
        _options = options;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
        _webSocketConnectionLogger = webSocketConnectionLogger;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection?.Dispose();
    }

    /// <inheritdoc/>
    public async Task Connect()
    {
        _logger.Connecting(_options.Endpoint.ToString());
        _connection = new WebSocketConnection(_options.Endpoint, _webSocketConnectionLogger);
        await _connection.Connect();
    }

    /// <inheritdoc/>
    public async Task<CommandResult> PerformCommand(string route, object? command = null)
    {
        _logger.PerformingCommand(_options.Endpoint.ToString(), route);

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
}
