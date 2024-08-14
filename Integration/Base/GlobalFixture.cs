// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Base;

/// <summary>
/// Represents a global fixture for the test run.
/// </summary>
public class GlobalFixture : IAsyncDisposable
{
    public const string HostName = "mongo";

    /// <summary>
    /// Initializes a new instance of <see cref="GlobalFixture"/>.
    /// </summary>
    public GlobalFixture()
    {
        Directory.CreateDirectory("backups");

        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        MongoDBContainer = new ContainerBuilder()
            .WithImage("cratis/mongodb")
            .WithPortBinding(27018, 27017)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(Network)
            .Build();

        MongoDBContainer.StartAsync().GetAwaiter().GetResult();

        EventStore = new MongoDBDatabase(MongoDBContainer, "testing+es");
        EventStoreForNamespace = new MongoDBDatabase(MongoDBContainer, "testing+es+Default");
        ReadModels = new MongoDBDatabase(MongoDBContainer, "testing");
    }

    /// <summary>
    /// Get the container network.
    /// </summary>
    public INetwork Network { get; }

    /// <summary>
    /// Get the MongoDB container.
    /// </summary>
    public IContainer MongoDBContainer { get; }

    public MongoDBDatabase EventStore { get; }
    public MongoDBDatabase EventStoreForNamespace { get; }
    public MongoDBDatabase ReadModels { get; }

    public async ValueTask DisposeAsync()
    {
        await MongoDBContainer.DisposeAsync();
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
        var mongoClient = new MongoClient($"mongodb://{MongoDBContainer.Hostname}:{MongoDBContainer.GetMappedPublicPort(27017)}");
        var namesCursor = await mongoClient.ListDatabaseNamesAsync();
        var names = await namesCursor.ToListAsync();
        foreach (var name in names)
        {
            if (name == "admin" || name == "config" || name == "local") continue;
            await mongoClient.DropDatabaseAsync(name);
        }
    }
}
