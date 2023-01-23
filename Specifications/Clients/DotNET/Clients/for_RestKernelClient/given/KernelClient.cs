// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Tasks;
using Aksio.Cratis.Timers;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients.for_RestKernelClient.given;

public class KernelClient : RestKernelClient
{
    internal readonly Mock<HttpClient> http_client;

    public KernelClient(
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
        http_client = new();
    }

    protected override HttpClient CreateHttpClient() => http_client.Object;
}
