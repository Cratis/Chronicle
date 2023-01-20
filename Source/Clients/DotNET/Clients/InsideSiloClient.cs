// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Commands;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Queries;
using Aksio.Cratis.Timers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClient"/> for usage inside a silo.
/// </summary>
public class InsideSiloClient : IClient
{
    readonly SingleKernelClient _innerClient;

    public InsideSiloClient(
        IServer server,
        IHttpClientFactory httpClientFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        IClientLifecycle clientLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<SingleKernelClient> logger)
    {
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var endpoint = new Uri(addresses!.Addresses.First());
        var options = new SingleKernelOptions
        {
            Endpoint = endpoint
        };
        _innerClient = new(
            httpClientFactory,
            timerFactory,
            executionContextManager,
            options,
            endpoint,
            clientLifecycle,
            jsonSerializerOptions,
            logger);
    }

    /// <inheritdoc/>
    public bool IsConnected => _innerClient.IsConnected;

    /// <inheritdoc/>
    public ConnectionId ConnectionId => _innerClient.ConnectionId;

    /// <inheritdoc/>
    public Task Connect() => _innerClient.Connect();

    /// <inheritdoc/>
    public Task<CommandResult> PerformCommand(string route, object? command = null) => _innerClient.PerformCommand(route, command);

    /// <inheritdoc/>
    public Task<QueryResult> PerformQuery(string route, IDictionary<string, string>? queryString = null) => _innerClient.PerformQuery(route, queryString);
}
