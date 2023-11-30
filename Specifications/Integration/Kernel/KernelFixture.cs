// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Aksio.Cratis.Kernel;

public class KernelFixture : IDisposable
{
    public const string HostName = "cratis";

    string _name = string.Empty;

    public KernelFixture(GlobalFixture globalFixture)
    {
        var dockerBuildContextPath = DirectoryHelper.GetDirectoryThatHasSubDirectory(".git");

        const string imageName = "cratis";

        var image = new ImageFromDockerfileBuilder()
            .WithName(imageName)
            .WithDockerfileDirectory(dockerBuildContextPath)
            .WithDockerfile("Dockerfile.Kernel")
            .WithBuildArgument("TC_REUSABLE", "true")
            .Build();
        image.CreateAsync().GetAwaiter().GetResult();

        KernelContainer = new ContainerBuilder()
            .WithImage(imageName)
            .WithPortBinding(8080, 80)
            .WithPortBinding(8081)
            .WithPortBinding(11111)
            .WithPortBinding(30000)
            .WithHostname(HostName)
            .WithNetwork(globalFixture.Network)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "cratis.json"), "/App/cratis.json")
            .Build();

        KernelContainer.StartAsync().GetAwaiter().GetResult();
        GlobalFixture = globalFixture;
    }

    public IContainer KernelContainer { get; }
    public INetwork Network => GlobalFixture.Network;
    public MongoDBDatabase Cluster => GlobalFixture.Cluster;
    public MongoDBDatabase SharedEventStore => GlobalFixture.SharedEventStore;
    public MongoDBDatabase EventStore => GlobalFixture.EventStore;
    public MongoDBDatabase ReadModels => GlobalFixture.ReadModels;
    public GlobalFixture GlobalFixture { get; }

    public void SetName(string name) => _name = name;

    public virtual void Dispose()
    {
        GlobalFixture.PerformBackup(_name);
        GlobalFixture.RemoveAllDatabases().GetAwaiter().GetResult();

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

public static class DirectoryHelper
{
    public static string GetDirectoryThatHasSubDirectory(string subDirectoryToLookFor)
    {
        var current = Directory.GetCurrentDirectory();
        do
        {
            if (Directory.GetDirectories(current).Any(_ => _.EndsWith(subDirectoryToLookFor))) break;
            current = Directory.GetParent(current)?.FullName;
        } while (current != null);
        return current;
    }
}
