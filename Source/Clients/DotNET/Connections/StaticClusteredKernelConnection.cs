// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Net;
using Aksio.Tasks;
using Aksio.Timers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents a <see cref="IConnection"/> for a clustered Kernel.
/// </summary>
public class StaticClusteredKernelConnection : ClusteredKernelClient
{
    readonly IOptions<ClientOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusteredKernelClient"/> class.
    /// </summary>
    /// <param name="options">The <see cref="ClientOptions"/>.</param>
    /// <param name="server">The ASP.NET Core server.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting service instances.</param>
    /// <param name="httpClientFactory">The <see cref="ILoadBalancedHttpClientFactory"/> to use.</param>
    /// <param name="taskFactory">A <see cref="ITaskFactory"/> for creating tasks.</param>
    /// <param name="timerFactory">A <see cref="ITimerFactory"/> for creating timers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="connectionLifecycle"><see cref="IConnectionLifecycle"/> for communicating lifecycle events outside.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public StaticClusteredKernelConnection(
        IOptions<ClientOptions> options,
        IServer server,
        IServiceProvider serviceProvider,
        ILoadBalancedHttpClientFactory httpClientFactory,
        ITaskFactory taskFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        IConnectionLifecycle connectionLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
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
    }

    /// <inheritdoc/>
    protected override IEnumerable<Uri> Endpoints => _options.Value.Kernel.StaticCluster?.Endpoints ?? Enumerable.Empty<Uri>();
}
