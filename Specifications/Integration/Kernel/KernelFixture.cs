// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Client;
using Aksio.Cratis.EventSequences;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel;

public class KernelFixture : IDisposable
{
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly IClient _client;

    public KernelFixture()
    {
        KernelContainer = new ContainerBuilder()
            .WithImage("aksioinsurtech/cratis:9.11.0-beta.2")
            .WithPortBinding(8080, 80)
            .WithPortBinding(8081)
            .WithPortBinding(11111)
            .WithPortBinding(30000)
            .WithNetwork(GlobalFixture.Network)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "cratis.json"), "/app/cratis.json")
            .Build();

        KernelContainer.StartAsync().GetAwaiter().GetResult();

#pragma warning disable CA2000 // Dispose objects before losing scope
        var webAppBuilder = WebApplication.CreateBuilder()
            .UseCratis(_ => _
                .ForMicroservice(MicroserviceId.Unspecified, "Test"),
                loggerFactory: new NullLoggerFactory());

        webAppBuilder.Logging.ClearProviders();
        var webApp = webAppBuilder.Build();
#pragma warning restore CA2000 // Dispose objects before losing scope
        webApp.UseCratis();

        ExecutionContextManager = webApp.Services.GetService<IExecutionContextManager>();

        webApp.StartAsync(_cancellationTokenSource.Token);
        EventLog = webApp.Services.GetRequiredService<IEventLog>();
        _client = webApp.Services.GetRequiredService<IClient>();

        Task.Delay(5000).GetAwaiter().GetResult();
    }

    public IContainer KernelContainer { get; }
    public IEventLog EventLog { get; }
    public IExecutionContextManager ExecutionContextManager { get; }

    public void Dispose()
    {
        ExecutionContextManager.Establish(TenantId.Development, CorrelationId.New(), MicroserviceId.Unspecified);
        _client.Disconnect().GetAwaiter().GetResult();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        KernelContainer.StopAsync().GetAwaiter().GetResult();
#pragma warning disable CA2012 // Use ValueTasks correctly
        var disposeTask = KernelContainer.DisposeAsync();
        if (!disposeTask.IsCompleted)
        {
            disposeTask.GetAwaiter().GetResult();
        }
#pragma warning restore CA2012 // Use ValueTasks correctly
    }
}
