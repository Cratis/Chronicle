// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Net;
using Aksio.Cratis.Tasks;
using Aksio.Cratis.Timers;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents a <see cref="ClusteredKernelClient"/> for Orleans Azure Table store cluster info.
/// </summary>
public class OrleansAzureTableStoreKernelClient : ClusteredKernelClient
{
    readonly AzureStorageClusterOptions _options;
    readonly ILogger<OrleansAzureTableStoreKernelClient> _clientLogger;
    IEnumerable<Uri> _endpoints = Enumerable.Empty<Uri>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusteredKernelClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The <see cref="ILoadBalancedHttpClientFactory"/> to use.</param>
    /// <param name="options">The <see cref="AzureStorageClusterOptions"/>.</param>
    /// <param name="taskFactory">A <see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="clientEndpoint">The client endpoint.</param>
    /// <param name="clientLifecycle"><see cref="IClientLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="clientLogger">The <see cref="ILogger"/> for this client.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public OrleansAzureTableStoreKernelClient(
        ILoadBalancedHttpClientFactory httpClientFactory,
        AzureStorageClusterOptions options,
        ITaskFactory taskFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        Uri clientEndpoint,
        IClientLifecycle clientLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<OrleansAzureTableStoreKernelClient> clientLogger,
        ILogger<RestKernelClient> logger) : base(
            httpClientFactory,
            taskFactory,
            timerFactory,
            executionContextManager,
            clientEndpoint,
            clientLifecycle,
            jsonSerializerOptions,
            logger)
    {
        _options = options;
        _clientLogger = clientLogger;
        RefreshSilos();
        timerFactory.Create(_ => RefreshSilos(), 30000, 30000);
    }

    protected override IEnumerable<Uri> Endpoints => _endpoints;

    void RefreshSilos()
    {
        _clientLogger.GettingSilosFromStorage();
        var client = new TableClient(
            _options.ConnectionString,
            _options.TableName);

        var result = client.Query<OrleansSiloInfo>(filter: "Status eq 'Active'");
        _endpoints = result.Select(_ => new Uri($"{(_options.Secure ? "https" : "http")}://{_.Address}:{_options.Port}")).ToArray().AsEnumerable();

        _clientLogger.UsingEndpoints(string.Join(", ", _endpoints));
    }
}
