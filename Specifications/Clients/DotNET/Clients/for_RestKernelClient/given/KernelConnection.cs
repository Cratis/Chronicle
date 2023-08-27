// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Connections;
using Aksio.Tasks;
using Aksio.Timers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aksio.Cratis.Clients.for_RestKernelConnection.given;

public class KernelConnection : RestKernelConnection
{
    internal readonly Mock<HttpClient> http_client;
    internal bool should_connect = true;

    public KernelConnection(
        IOptions<ClientOptions> options,
        IServer server,
        IServiceProvider serviceProvider,
        ITaskFactory taskFactory,
        ITimerFactory timerFactory,
        IExecutionContextManager executionContextManager,
        IConnectionLifecycle connectionLifecycle,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<RestKernelConnection> logger) : base(
            options,
            server,
            serviceProvider,
            taskFactory,
            timerFactory,
            executionContextManager,
            connectionLifecycle,
            jsonSerializerOptions,
            logger)
    {
        http_client = new();
    }

    public override Task Connect()
    {
        if (should_connect)
        {
            return base.Connect();
        }

        return Task.CompletedTask;
    }

    protected override HttpClient CreateHttpClient() => http_client.Object;
}
