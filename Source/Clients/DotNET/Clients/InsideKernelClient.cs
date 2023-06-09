// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Commands;
using Aksio.Cratis.Configuration;
using Aksio.Net;
using Aksio.Tasks;
using Aksio.Timers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClient"/> for usage inside a silo.
/// </summary>
public class InsideKernelClient : IClient
{
    readonly SingleKernelClient _innerClient;

    /// <inheritdoc/>
    public bool IsConnected => _innerClient.IsConnected;

    /// <inheritdoc/>
    public ConnectionId ConnectionId => _innerClient.ConnectionId;

    /// <summary>
    /// Initializes a new instance of the <see cref="InsideKernelClient"/> class.
    /// </summary>
    /// <param name="server">The ASP.NET Core <see cref="IServer"/>.</param>
    /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> to use.</param>
    /// <param name="taskFactory">A <see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="clientLifecycle"><see cref="IClientLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="singleKernelClientLogger"><see cref="ILogger"/> for single kernel client logging.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public InsideKernelClient(
        IServer server,
        IHttpClientFactory httpClientFactory,
        ITaskFactory taskFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        IClientLifecycle clientLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<SingleKernelClient> singleKernelClientLogger,
        ILogger<InsideKernelClient> logger)
    {
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var endpoint = addresses!.GetFirstAddressAsUri();
        logger.ConnectingKernel(endpoint);

        var options = new SingleKernelOptions
        {
            Endpoint = endpoint
        };
        _innerClient = new(
            httpClientFactory,
            options,
            taskFactory,
            timerFactory,
            executionContextManager,
            endpoint,
            clientLifecycle,
            jsonSerializerOptions,
            singleKernelClientLogger);
    }

    /// <inheritdoc/>
    public Task Connect() => _innerClient.Connect();

    /// <inheritdoc/>
    public Task Disconnect() => _innerClient.Disconnect();

    /// <inheritdoc/>
    public Task<CommandResult> PerformCommand(string route, object? command = null, object? metadata = default) => _innerClient.PerformCommand(route, command, metadata);

    /// <inheritdoc/>
    public Task<TypedQueryResult<TResult>> PerformQuery<TResult>(string route, IDictionary<string, string>? queryString = null, object? metadata = default) => _innerClient.PerformQuery<TResult>(route, queryString, metadata);
}
