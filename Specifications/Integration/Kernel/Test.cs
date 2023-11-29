// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using DotNet.Testcontainers.Builders;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel;

[Collection(GlobalCollection.Name)]
public class Test : IClassFixture<KernelFixture>
{
    readonly GlobalFixture _globalFixture;
    readonly KernelFixture _kernelFixture;

    public Test(
        KernelFixture kernelFixture,
        GlobalFixture globalFixture)
    {
        _globalFixture = globalFixture;
        _kernelFixture = kernelFixture;

        var imageName = "basic";

        var current = Directory.GetCurrentDirectory();
        do
        {
            if (Directory.GetDirectories(current).Any(_ => _.EndsWith("TestClients"))) break;
            current = Directory.GetParent(current)?.FullName;
        } while (current != null);

        var clientPath = Path.Combine(current, "TestClients", "Basic");

        var containerImage = new ImageFromDockerfileBuilder()
            .WithName(imageName)
            .WithDockerfileDirectory(clientPath)
            .WithDockerfile("Dockerfile")
            .Build();

        containerImage.CreateAsync().GetAwaiter().GetResult();

        var container = new ContainerBuilder()
            .WithImage(imageName)
            .WithNetwork(GlobalFixture.Network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Horse"))
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "appSettings.client.json"), "/app/appSettings.json")
            .Build();

        container.StartAsync().GetAwaiter().GetResult();

        _globalFixture.EventStore.Database.GetCollection<BsonDocument>("event-log").Find(_ => true).ToList();

        _globalFixture.EventStore.Changes
            .Subscribe(_ => Console.WriteLine("Got event store change"));

        _globalFixture.EventStore.Changes
             .Where(_ => _.OperationType == ChangeStreamOperationType.Insert)
             .Subscribe(_ => Console.WriteLine("Got insert"));
    }

    [Fact]
    async Task DoStuff()
    {
        await Task.Delay(5000);
        Assert.True(true);
    }
}
