// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Configuration;
using Aksio.Tasks;
using Aksio.Timers;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClient"/> for a single instance.
/// </summary>
public class SingleKernelClient : RestKernelClient
{
    readonly IHttpClientFactory _httpClientFactory;
    readonly SingleKernelOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleKernelClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> to use.</param>
    /// <param name="options">The <see cref="SingleKernelOptions"/> with specific configuration.</param>
    /// <param name="taskFactory">A <see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="clientEndpoint">The client endpoint.</param>
    /// <param name="clientLifecycle"><see cref="IClientLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public SingleKernelClient(
        IHttpClientFactory httpClientFactory,
        SingleKernelOptions options,
        ITaskFactory taskFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        Uri clientEndpoint,
        IClientLifecycle clientLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<RestKernelClient> logger) : base(
            taskFactory,
            timerFactory,
            executionContextManager,
            clientEndpoint,
            clientLifecycle,
            jsonSerializerOptions,
            logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    /// <inheritdoc/>
    protected override HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = _options.Endpoint;
        return client;
    }
}
