// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Docker.DotNet;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
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
    readonly object _mongoDBInitializationLock = new();
#else
    readonly Lock _containerLock = new();
    readonly Lock _mongoDBInitializationLock = new();
#endif

    MongoDBDatabase? _eventStore;
    MongoDBDatabase? _eventStoreForNamespace;
    MongoDBDatabase? _readModels;
    IContainer? _container;
    bool _mongoDBInitialized;
    bool _started;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleFixture"/> class.
    /// </summary>
    protected ChronicleFixture()
    {
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        Directory.CreateDirectory("backups");
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
    }

    /// <inheritdoc/>
    public virtual string MongoDBServer
    {
        get
        {
            EnsureMongoDBInitialized();
            return $"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true";
        }
    }

    /// <summary>
    /// Get the MongoDB container.
    /// </summary>
    public virtual IContainer MongoDBContainer
    {
        get
        {
            EnsureMongoDBInitialized();
            return _container!;
        }
    }

    /// <inheritdoc/>
    public INetwork Network { get; }

    /// <inheritdoc/>
    public MongoDBDatabase EventStore
    {
        get
        {
            EnsureMongoDBInitialized();
            return _eventStore ??= new(MongoDBServer, Constants.EventStoreDatabaseName);
        }
    }

    /// <inheritdoc/>
    public MongoDBDatabase EventStoreForNamespace
    {
        get
        {
            EnsureMongoDBInitialized();
            return _eventStoreForNamespace ??= new(MongoDBServer, Constants.EventStoreNamespaceDatabaseName);
        }
    }

    /// <inheritdoc/>
    public MongoDBDatabase ReadModels
    {
        get
        {
            EnsureMongoDBInitialized();
            return _readModels ??= new(MongoDBServer, Constants.ReadModelsDatabaseName);
        }
    }

    /// <summary>
    /// Gets the logger factory for creating loggers.
    /// </summary>
    protected ILoggerFactory LoggerFactory { get; }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        await (_container?.DisposeAsync() ?? ValueTask.CompletedTask);
        await Network.DisposeAsync();
    }

    /// <inheritdoc/>
    public virtual async Task PerformBackupAsync(string? prefix = null)
    {
        EnsureMongoDBInitialized();

        prefix ??= string.Empty;
        if (!string.IsNullOrEmpty(prefix))
        {
            prefix = $"{prefix}-";
        }

        var backupName = $"{prefix}{DateTimeOffset.Now:yyyyMMdd-HHmmss}.tgz";
        try
        {
            await MongoDBContainer.ExecAsync(
            [
                "mongodump",
                $"--archive=/backups/{backupName}",
                "--gzip"
            ]);
        }
        catch
        {
        }
    }

    /// <inheritdoc/>
    public virtual async Task RemoveAllDatabases(IEnumerable<string>? excludePrefixes = null)
    {
        EnsureMongoDBInitialized();
        var settings = MongoClientSettings.FromConnectionString(MongoDBServer);

        using var mongoClient = new MongoClient(settings);
        var namesCursor = await mongoClient.ListDatabaseNamesAsync();
        var names = await namesCursor.ToListAsync();
        var systemNames = new[] { "admin", "config", "local" };
        foreach (var name in names.Where(name =>
            !systemNames.Contains(name) &&
            excludePrefixes?.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)) != true))
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

    /// <summary>
    /// Initializes the MongoDB server for the fixture.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual async Task InitializeMongoDB()
    {
        IContainer container;
        lock (_containerLock)
        {
            _container ??= BuildContainer(Network);
            container = _container;
        }

        await StartContainer(container);
    }

    /// <summary>
    /// Ensures that MongoDB has been initialized.
    /// </summary>
    protected void EnsureMongoDBInitialized()
    {
        if (_mongoDBInitialized) return;

        lock (_mongoDBInitializationLock)
        {
            if (_mongoDBInitialized) return;
            InitializeMongoDB().GetAwaiter().GetResult();
            _mongoDBInitialized = true;
        }
    }

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
            catch (Exception e) when (e is DockerApiException || e.InnerException is DockerApiException || e is TimeoutException)
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
