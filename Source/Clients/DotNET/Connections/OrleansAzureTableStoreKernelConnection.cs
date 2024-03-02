// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Net;
using Aksio.Tasks;
using Aksio.Timers;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents a <see cref="ClusteredKernelClient"/> for Orleans Azure Table store cluster info.
/// </summary>
public class OrleansAzureTableStoreKernelConnection : ClusteredKernelClient
{
    readonly IOptions<ClientOptions> _options;
    readonly ILogger<OrleansAzureTableStoreKernelConnection> _clientLogger;
    IEnumerable<Uri> _endpoints = Enumerable.Empty<Uri>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusteredKernelClient"/> class.
    /// </summary>
    /// <param name="options">The <see cref="ClientOptions"/>.</param>
    /// <param name="server">The ASP.NET Core <see cref="IServer"/>.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting service instances.</param>
    /// <param name="httpClientFactory">The <see cref="ILoadBalancedHttpClientFactory"/> to use.</param>
    /// <param name="taskFactory">A <see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="connectionLifecycle"><see cref="IConnectionLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="clientLogger">The <see cref="ILogger"/> for this client.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public OrleansAzureTableStoreKernelConnection(
        IOptions<ClientOptions> options,
        IServer server,
        IServiceProvider serviceProvider,
        ILoadBalancedHttpClientFactory httpClientFactory,
        ITaskFactory taskFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        IConnectionLifecycle connectionLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<OrleansAzureTableStoreKernelConnection> clientLogger,
        ILogger<RestKernelConnection> logger) : base(
            options,
            server,
            serviceProvider,
            httpClientFactory,
            taskFactory,
            timerFactory,
            executionContextManager,
            connectionLifecycle,
            jsonSerializerOptions,
            logger)
    {
        _options = options;
        _clientLogger = clientLogger;
        RefreshSilos();
        timerFactory.Create(_ => RefreshSilos(), 30000, 30000);
    }

    /// <inheritdoc/>
    protected override IEnumerable<Uri> Endpoints => _endpoints;

    /// <inheritdoc/>
    protected override Task OnDisconnected()
    {
        RefreshSilos();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnKernelUnavailable()
    {
        RefreshSilos();
        return Task.CompletedTask;
    }

    void RefreshSilos()
    {
        var options = _options.Value.Kernel.AzureStorageCluster!;
        _clientLogger.GettingSilosFromStorage();
        var client = new TableClient(
            options.ConnectionString,
            options.TableName);

        var result = client.Query<OrleansSiloInfo>(filter: "Status eq 'Active'");
        _endpoints = result.Select(_ => new Uri($"{(options.Secure ? "https" : "http")}://{_.Address}:{options.Port}")).ToArray().AsEnumerable();

        _clientLogger.UsingEndpoints(string.Join(", ", _endpoints));
    }
}
