// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel;

public class GlobalFixture : IDisposable
{
    internal static readonly INetwork Network;

    static GlobalFixture()
    {
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
    }

    public IContainer MongoDBContainer { get; }
    public Subject<ChangeStreamDocument<BsonDocument>> Changes { get; } = new();

    public GlobalFixture()
    {
        Directory.CreateDirectory("backups");

        MongoDBContainer = new ContainerBuilder()
            .WithImage("aksioinsurtech/mongodb")
            .WithPortBinding(27017)
            .WithHostname("mongo")
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(Network)
            .Build();

        MongoDBContainer.StartAsync().GetAwaiter().GetResult();

        Cluster = new MongoDBDatabase(MongoDBContainer, "cratis-shared");
        SharedEventStore = new MongoDBDatabase(MongoDBContainer, "event-store-shared");
        EventStore = new MongoDBDatabase(MongoDBContainer, "dev-event-store");
        ReadModels = new MongoDBDatabase(MongoDBContainer, "dev-read-models");
    }

    public MongoDBDatabase Cluster { get; }
    public MongoDBDatabase SharedEventStore { get; }
    public MongoDBDatabase EventStore { get; }
    public MongoDBDatabase ReadModels { get; }

    public void Dispose()
    {
        MongoDBContainer.ExecAsync(new[]
        {
            "mongodump",
            $"--archive=/backups/{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.tgz",
            "--gzip"
        }).GetAwaiter().GetResult();

        MongoDBContainer.StopAsync().GetAwaiter().GetResult();
#pragma warning disable CA2012 // Use ValueTasks correctly
        var disposeTask = MongoDBContainer.DisposeAsync();
        if (!disposeTask.IsCompleted)
        {
            disposeTask.GetAwaiter().GetResult();
        }
#pragma warning restore CA2012 // Use ValueTasks correctly
    }
}
