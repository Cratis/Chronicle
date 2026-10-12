// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Docker.DotNet;
using Docker.DotNet.Models;
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
#else
    readonly Lock _containerLock = new();
#endif

    MongoDBContextStorage? _contextStorage;
    MongoDBDatabase? _eventStore;
    MongoDBDatabase? _eventStoreForNamespace;
    MongoDBDatabase? _readModels;
    IContainer? _container;
    bool _started;
    bool _reserveKernelPorts = true;

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

        // MongoDBContainer is virtual so derived fixtures can swap the container source.
        // The override is the documented extension point; the base ctor must call it to
        // trigger the lazy build-and-start cycle that every fixture relies on.
#pragma warning disable MA0056
        StartContainer(MongoDBContainer).GetAwaiter().GetResult();
#pragma warning restore MA0056
    }

    /// <summary>
    /// Get the MongoDB container.
    /// </summary>
    public virtual IContainer MongoDBContainer
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
    public MongoDBDatabase EventStore => _contextStorage?.EventStore ?? (_eventStore ??= new(MongoDBContainer, Constants.EventStoreDatabaseName));

    /// <inheritdoc/>
    public MongoDBDatabase EventStoreForNamespace => _contextStorage?.EventStoreForNamespace ?? (_eventStoreForNamespace ??= new(MongoDBContainer, Constants.EventStoreNamespaceDatabaseName));

    /// <inheritdoc/>
    public MongoDBDatabase ReadModels => _contextStorage?.ReadModels ?? (_readModels ??= new(MongoDBContainer, Constants.ReadModelsDatabaseName));

    /// <summary>
    /// Gets a value indicating whether each context owns a fresh in-process host with isolated MongoDB storage.
    /// </summary>
    /// <remarks>
    /// Opt in only for in-process MongoDB fixtures. The default is false; out-of-process and remote kernels
    /// continue to use the existing kernel reset. Issued databases are dropped when this collection fixture is disposed.
    /// The prefix is applied by the default MongoDB storage configuration; a fixture that overrides
    /// <c language="csharp">GetStorageConfigurator</c> must pass the client fixture's <c language="csharp">DatabaseNamePrefix</c> to <c language="csharp">WithMongoDB</c> itself.
    /// </remarks>
    public virtual bool UsesPerContextMongoDBHost => false;

    /// <summary>
    /// Gets the logger factory for creating loggers.
    /// </summary>
    protected ILoggerFactory LoggerFactory { get; }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        if (_contextStorage is null)
        {
            await (_container?.DisposeAsync() ?? ValueTask.CompletedTask);
            await Network.DisposeAsync();
            return;
        }

        try
        {
            await DropContextDatabases();
        }
        finally
        {
            try
            {
                _contextStorage.ResetReaders();
            }
            finally
            {
                try
                {
                    await (_container?.DisposeAsync() ?? ValueTask.CompletedTask);
                }
                finally
                {
                    await Network.DisposeAsync();
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual async Task PerformBackupAsync(string? prefix = null)
    {
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
        var urlBuilder = new MongoUrlBuilder($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}")
        {
            DirectConnection = true
        };
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());

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
    /// Restarts the MongoDB server so that client reconnection behavior can be tested.
    /// </summary>
    /// <remarks>
    /// The default implementation stops and starts the <see cref="MongoDBContainer"/>.
    /// Subclasses may override this to use a lighter-weight mechanism (e.g. killing only
    /// the mongod process inside the container) when stopping the container would destroy
    /// storage that must survive the restart.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public virtual async Task RestartMongoDBAsync()
    {
        await MongoDBContainer.StopAsync();
        await MongoDBContainer.StartAsync();
    }

    /// <summary>
    /// Starts a fresh MongoDB storage identity for a test context.
    /// </summary>
    /// <returns>The prefix for the new context.</returns>
    internal string BeginMongoDBContext()
    {
        _contextStorage ??= new(name => new(MongoDBContainer, name));
        return _contextStorage.BeginContext();
    }

    /// <summary>
    /// Builds the container with the specified network.
    /// </summary>
    /// <param name="network">The network to use.</param>
    /// <returns>The built container.</returns>
    protected abstract IContainer BuildContainer(INetwork network);

    /// <summary>
    /// Reserves the kernel's listening ports from ephemeral allocation when the container runtime supports it.
    /// </summary>
    /// <param name="builder">The kernel container builder.</param>
    /// <returns>The builder with the reservation applied when supported.</returns>
    protected ContainerBuilder WithReservedKernelPorts(ContainerBuilder builder) => _reserveKernelPorts
        ? builder.WithCreateParameterModifier(parameters =>
        {
            var hostConfig = parameters.HostConfig ?? new HostConfig();
            var sysctls = hostConfig.Sysctls ?? new Dictionary<string, string>();
            sysctls["net.ipv4.ip_local_reserved_ports"] = "11111,30000,35000";
            hostConfig.Sysctls = sysctls;
            parameters.HostConfig = hostConfig;
        })
        : builder;

    async Task DropContextDatabases()
    {
        var urlBuilder = new MongoUrlBuilder($"mongodb://{MongoDBContainer.Hostname}:{MongoDBContainer.GetMappedPublicPort(27017)}")
        {
            DirectConnection = true
        };
        using var client = new MongoClient(MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl()));
        await _contextStorage!.DropDatabases(client);
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
                // Rootless and some non-Docker runtimes reject this sysctl at container creation.
                // Retry without it only for an explicit rejection; other startup errors retain their usual handling.
                if (_reserveKernelPorts && (e is DockerApiException || e.InnerException is DockerApiException)
                    && (e.Message.Contains("sysctl", StringComparison.OrdinalIgnoreCase)
                        || e.InnerException?.Message.Contains("sysctl", StringComparison.OrdinalIgnoreCase) == true))
                {
                    _reserveKernelPorts = false;
                    Console.WriteLine("Container runtime rejected the reserved-port sysctl; retrying without it.");
                }

                Console.WriteLine($"Failed to start the container: {e.Message} - retrying...");
                failure = e;

                // Rebuild before retrying. A container instance holds the host port it was built with, so a
                // bind-time conflict is identical on every attempt - all the retries fail the same way, none of
                // them says a port is the reason, and the tier is wedged for as long as the retries take.
                // Rebuilding allocates a fresh port, which is the only thing that can make the retry mean
                // anything.
                container = BuildContainer(Network);
                _container = container;

                await Task.Delay(2000);
            }

            try
            {
                var logs = await container.GetLogsAsync();
                Console.WriteLine(logs.Stdout);
                Console.WriteLine(logs.Stderr);
            }
            catch (Exception logException)
            {
                Console.WriteLine($"Could not retrieve container logs: {logException.Message}");
            }
        }
        while (failure is not null && ++retryCount < 10);

        if (failure is not null)
        {
            // Surfacing the underlying failure prevents downstream code from operating on an
            // unstarted container. Without throwing, the fixture would silently continue and
            // every subsequent GetMappedPublicPort call would surface as a confusing
            // ArgumentOutOfRangeException far away from the real cause.
            throw new InvalidOperationException(
                $"Failed to start container '{container.Image.FullName}' after {retryCount} attempts.",
                failure);
        }

        _started = true;
        Console.WriteLine("We have started the container successfully.");
    }
}
