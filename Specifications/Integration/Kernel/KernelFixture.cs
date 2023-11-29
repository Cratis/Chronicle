// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Aksio.Cratis.Kernel;

public class KernelFixture : IDisposable
{
    public KernelFixture()
    {
        KernelContainer = new ContainerBuilder()
            .WithImage("aksioinsurtech/cratis:9.11.0-beta.2")
            .WithPortBinding(8080, 80)
            .WithPortBinding(8081)
            .WithPortBinding(11111)
            .WithPortBinding(30000)
            .WithHostname("cratis")
            .WithNetwork(GlobalFixture.Network)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "cratis.json"), "/app/cratis.json")
            .Build();

        KernelContainer.StartAsync().GetAwaiter().GetResult();
    }

    public IContainer KernelContainer { get; }

    public virtual void Dispose()
    {
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
