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

#if NET8_0
    readonly object _containerLock = new();
#else
    readonly Lock _containerLock = new();
#endif

    MongoDBDatabase? _eventStore;
    MongoDBDatabase? _eventStoreForNamespace;
    MongoDBDatabase? _readModels;
    IContainer? _container;
    bool _started;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleFixture"/> class.
    /// </summary>
    protected ChronicleFixture()
    {
        Directory.CreateDirectory("backups");
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        StartContainer(MongoDBContainer).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the MongoDB container.
    /// </summary>
    public IContainer MongoDBContainer
    {
        get
        {
            lock (_containerLock)
            {
                if (_container is null)
                {
                    _container = BuildContainer(Network);
                    StartContainer(_container).GetAwaiter().GetResult();
                }
                return _container;
            }
        }
    }

    /// <inheritdoc/>
    public INetwork Network { get; }

    /// <inheritdoc/>
    public MongoDBDatabase EventStore => _eventStore ??= new(MongoDBContainer, Constants.EventStoreDatabaseName);

    /// <inheritdoc/>
    public MongoDBDatabase EventStoreForNamespace => _eventStoreForNamespace ??= new(MongoDBContainer, Constants.EventStoreNamespaceDatabaseName);

    /// <inheritdoc/>
    public MongoDBDatabase ReadModels => _readModels ??= new(MongoDBContainer, Constants.ReadModelsDatabaseName);

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        await (_container?.DisposeAsync() ?? ValueTask.CompletedTask);
        await Network.DisposeAsync();
    }

    /// <inheritdoc/>
    public virtual void PerformBackup(string? prefix = null)
    {
        prefix ??= string.Empty;
        if (!string.IsNullOrEmpty(prefix))
        {
            prefix = $"{prefix}-";
        }

        var backupName = $"{prefix}{DateTimeOffset.Now:yyyyMMdd-HHmmss}.tgz";
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
        var urlBuilder = new MongoUrlBuilder($"mongodb://{MongoDBContainer.Hostname}:{MongoDBContainer.GetMappedPublicPort(MongoDBPort)}")
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

    /// <summary>
    /// Builds the container with the specified network.
    /// </summary>
    /// <param name="network">The network to use.</param>
    /// <returns>The built container.</returns>
    protected abstract IContainer BuildContainer(INetwork network);

    async Task StartContainer(IContainer container)
    {
        if (_started) return;

        var retryCount = 0;
        Exception? failure;
        do
        {
            try
            {
                Console.WriteLine($"Starting container image '{container.Image.FullName}'...");

                failure = null;

                await container.StartAsync();
            }
            catch (Exception e) when (e is DockerApiException || e.InnerException is DockerApiException)
            {
                Console.WriteLine($"Failed to start the container: {e.Message} - retrying...");
                failure = e;
                await Task.Delay(2000);
            }

            var logs = await container.GetLogsAsync();
            Console.WriteLine(logs.Stdout);
            Console.WriteLine(logs.Stderr);
        }
        while (failure is not null && ++retryCount < 10);

        if (failure is not null)
        {
            Console.WriteLine($"Failed to start the container after {retryCount} attempts.");
        }
        else
        {
            _started = true;
            Console.WriteLine("We have started the container successfully.");
        }
    }
}
