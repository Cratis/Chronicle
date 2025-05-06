// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Docker.DotNet;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a global fixture for the test run.
/// </summary>
public class ChronicleFixture : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the Mongo container.
    /// </summary>
    public const string HostName = "mongo";

    /// <summary>
    /// Initializes a new instance of <see cref="ChronicleFixture"/>.
    /// </summary>
    public ChronicleFixture()
    {
        Directory.CreateDirectory("backups");
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        MongoDBContainer = new ContainerBuilder()
            .WithImage("mongo")
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(27018, 27017)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(Network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017))
            .Build();

        var retryCount = 0;
        Exception? failure;
        do
        {
            try
            {
                failure = null;
                MongoDBContainer.StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception e) when (e is DockerApiException || e.InnerException is DockerApiException)
            {
                failure = e;
                Task.Delay(2000).GetAwaiter().GetResult();
            }
        }
        while (failure is not null && ++retryCount < 10);

        EventStore = new MongoDBDatabase(MongoDBContainer, Constants.EventStoreDatabaseName);
        EventStoreForNamespace = new MongoDBDatabase(MongoDBContainer, Constants.EventStoreNamespaceDatabaseName);
        ReadModels = new MongoDBDatabase(MongoDBContainer, Constants.ReadModelsDatabaseName);
    }

    /// <summary>
    /// Get the container network.
    /// </summary>
    public INetwork Network { get; }

    /// <summary>
    /// Get the MongoDB container.
    /// </summary>
    public IContainer MongoDBContainer { get; }

    /// <summary>
    /// Gets the event store database.
    /// </summary>
    public MongoDBDatabase EventStore { get; }

    /// <summary>
    /// Gets the event store database for the namespace used in the event store.
    /// </summary>
    public MongoDBDatabase EventStoreForNamespace { get; }

    /// <summary>
    /// Gets the read models database.
    /// </summary>
    public MongoDBDatabase ReadModels { get; }

    /// <summary>
    /// Performs a backup of the MongoDB database.
    /// </summary>
    /// <param name="prefix">The prefix to use in the filename.</param>
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

    /// <summary>
    /// Clears all databases in the MongoDB container.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task RemoveAllDatabases()
    {
        var urlBuilder = new MongoUrlBuilder($"mongodb://{MongoDBContainer.Hostname}:{MongoDBContainer.GetMappedPublicPort(27017)}")
        {
            DirectConnection = true
        };
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());

        using var mongoClient = new MongoClient(settings);
        var namesCursor = await mongoClient.ListDatabaseNamesAsync();
        var names = await namesCursor.ToListAsync();
        foreach (var name in names)
        {
            if (name == "admin" || name == "config" || name == "local") continue;
            await mongoClient.DropDatabaseAsync(name);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await MongoDBContainer.DisposeAsync();
    }
}
