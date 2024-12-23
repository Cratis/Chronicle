// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Driver;

namespace Cratis.Chronicle;

public class GlobalFixture : IDisposable
{
    public const string HostName = "mongo";

    public GlobalFixture()
    {
        Directory.CreateDirectory("backups");

        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();


        MongoDBContainer = new ContainerBuilder()
            .WithImage("cratis/mongodb")
            .WithPortBinding(27017)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(Network)
            .Build();

        MongoDBContainer.StartAsync().GetAwaiter().GetResult();

        Cluster = new MongoDBDatabase(MongoDBContainer, "cratis-shared");
        SharedEventStore = new MongoDBDatabase(MongoDBContainer, "event-store-shared");
        EventStore = new MongoDBDatabase(MongoDBContainer, "dev-event-store");
        ReadModels = new MongoDBDatabase(MongoDBContainer, "dev-read-models");
    }

    public INetwork Network { get; }
    public IContainer MongoDBContainer { get; }
    public MongoDBDatabase Cluster { get; }
    public MongoDBDatabase SharedEventStore { get; }
    public MongoDBDatabase EventStore { get; }
    public MongoDBDatabase ReadModels { get; }

    public void Dispose()
    {
        MongoDBContainer.StopAsync().GetAwaiter().GetResult();
#pragma warning disable CA2012 // Use ValueTasks correctly
        var disposeTask = MongoDBContainer.DisposeAsync();
        if (!disposeTask.IsCompleted)
        {
            disposeTask.GetAwaiter().GetResult();
        }
#pragma warning restore CA2012 // Use ValueTasks correctly
    }

    public void PerformBackup(string? prefix = null)
    {
        prefix ??= string.Empty;
        if (!string.IsNullOrEmpty(prefix))
        {
            prefix = $"{prefix}-";
        }

        var backupName = $"{prefix}{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.tgz";
        MongoDBContainer.ExecAsync(
        [
            "mongodump",
            $"--archive=/backups/{backupName}",
            "--gzip"
        ]).GetAwaiter().GetResult();
    }

    public async Task RemoveAllDatabases()
    {
        var urlBuilder = new MongoUrlBuilder($"mongodb://{MongoDBContainer.Hostname}:{MongoDBContainer.GetMappedPublicPort(27017)}")
        {
            DirectConnection = true
        };
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
        var mongoClient = new MongoClient(settings);
        var namesCursor = await mongoClient.ListDatabaseNamesAsync();
        var names = await namesCursor.ToListAsync();
        foreach (var name in names)
        {
            if (name == "admin" || name == "config" || name == "local") continue;
            await mongoClient.DropDatabaseAsync(name);
        }
    }
}
