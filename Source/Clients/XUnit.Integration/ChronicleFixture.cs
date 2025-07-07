// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Docker.DotNet;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Driver;
namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents the base <see cref="IChronicleFixture"/>.
/// </summary>
public abstract class ChronicleFixture : IChronicleFixture
{
    /// <summary>
    /// The exposed MongoDb port.
    /// </summary>
    public const int MongoDBPort = 27018;

    MongoDBDatabase? _eventStore;
    MongoDBDatabase? _eventStoreForNamespace;
    MongoDBDatabase? _readModels;

    /// <summary>
    /// Gets the unique identifier for this fixture instance.
    /// </summary>
    public string UniqueId { get; } = Guid.NewGuid().ToString("D")[..8];

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleFixture"/> class.
    /// </summary>
    /// <param name="createMongoDBContainer">The factory for the mongodb container.</param>
    protected ChronicleFixture(Func<INetwork, IContainer> createMongoDBContainer)
    {
        Directory.CreateDirectory("backups");
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
        MongoDBContainer = createMongoDBContainer(Network);
        StartContainer(MongoDBContainer).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the MongoDB container.
    /// </summary>
    public IContainer MongoDBContainer { get; }

    /// <inheritdoc/>
    public INetwork Network { get; }

    /// <inheritdoc/>
    public MongoDBDatabase EventStore => _eventStore ??= new(MongoDBContainer, Constants.GetEventStoreDatabaseName(UniqueId));

    /// <inheritdoc/>
    public MongoDBDatabase EventStoreForNamespace => _eventStoreForNamespace ??= new(MongoDBContainer, Constants.GetEventStoreNamespaceDatabaseName(UniqueId));

    /// <inheritdoc/>
    public MongoDBDatabase ReadModels => _readModels ??= new(MongoDBContainer, Constants.GetReadModelsDatabaseName(UniqueId));

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        await MongoDBContainer.DisposeAsync();
    }

    /// <inheritdoc/>
    public virtual void PerformBackup(string? prefix = null)
    {
        prefix ??= string.Empty;
        if (!string.IsNullOrEmpty(prefix))
        {
            prefix = $"{prefix}-";
        }

        var backupName = $"{prefix}{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.tgz";
        try
        {
            MongoDBContainer.ExecAsync(
            [
                "mongodump",
                $"--archive=/backups/{backupName}",
                "--gzip"
            ]).GetAwaiter().GetResult();
        }
        catch
        {
        }
    }

    /// <inheritdoc/>
    public virtual async Task RemoveAllDatabases()
    {
        var urlBuilder = new MongoUrlBuilder($"mongodb://{MongoDBContainer.Hostname}:{MongoDBContainer.GetMappedPublicPort(27017)}")
        {
            DirectConnection = true
        };
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());

        using var mongoClient = new MongoClient(settings);
        var namesCursor = await mongoClient.ListDatabaseNamesAsync();
        var names = await namesCursor.ToListAsync();
        foreach (var name in names.Where(name => name != "admin" && name != "config" && name != "local"))
        {
            await mongoClient.DropDatabaseAsync(name);
        }
    }

    static async Task StartContainer(IContainer container)
    {
        var retryCount = 0;
        Exception? failure;
        do
        {
            try
            {
                failure = null;
                await container.StartAsync();
            }
            catch (Exception e) when (e is DockerApiException || e.InnerException is DockerApiException)
            {
                failure = e;
                await Task.Delay(2000);
            }
        }
        while (failure is not null && ++retryCount < 10);
    }
}
