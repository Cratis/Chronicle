// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Commands;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Net;
using Aksio.Tasks;
using Aksio.Timers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnection"/> for usage inside a silo.
/// </summary>
public class InsideKernelConnection : IConnection, IDisposable
{
    readonly SingleKernelConnection _innerClient;

    /// <inheritdoc/>
    public bool IsConnected => _innerClient.IsConnected;

    /// <inheritdoc/>
    public ConnectionId ConnectionId => _innerClient.ConnectionId;

    /// <summary>
    /// Initializes a new instance of the <see cref="InsideKernelConnection"/> class.
    /// </summary>
    /// <param name="options">The <see cref="ClientOptions"/>.</param>
    /// <param name="server">The ASP.NET Core <see cref="IServer"/>.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting service instances.</param>
    /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> to use.</param>
    /// <param name="taskFactory">A <see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="connectionLifecycle"><see cref="IConnectionLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="singleKernelClientLogger"><see cref="ILogger"/> for single kernel client logging.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public InsideKernelConnection(
        IOptions<ClientOptions> options,
        IServer server,
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ITaskFactory taskFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        IConnectionLifecycle connectionLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<SingleKernelConnection> singleKernelClientLogger,
        ILogger<InsideKernelConnection> logger)
    {
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var endpoint = addresses!.GetFirstAddressAsUri();
        endpoint = new Uri($"http://127.0.0.1:{endpoint.Port}");
        logger.InsideKernelConfigured(endpoint);

        options.Value.Kernel.SingleKernel = new SingleKernelOptions
        {
            Endpoint = endpoint
        };
        options.Value.Kernel.AdvertisedClientEndpoint = endpoint;
        _innerClient = new(
            options,
            server,
            serviceProvider,
            httpClientFactory,
            taskFactory,
            timerFactory,
            executionContextManager,
            connectionLifecycle,
            jsonSerializerOptions,
            singleKernelClientLogger);
    }

    /// <inheritdoc/>
    public void Dispose() => _innerClient.Dispose();

    /// <inheritdoc/>
    public Task Connect() => _innerClient.Connect();

    /// <inheritdoc/>
    public Task Disconnect() => _innerClient.Disconnect();

    /// <inheritdoc/>
    public Task<CommandResult> PerformCommand(string route, object? command = null, object? metadata = default) => _innerClient.PerformCommand(route, command, metadata);

    /// <inheritdoc/>
    public Task<TypedQueryResult<TResult>> PerformQuery<TResult>(string route, IDictionary<string, string>? queryString = null, object? metadata = default) => _innerClient.PerformQuery<TResult>(route, queryString, metadata);
}
