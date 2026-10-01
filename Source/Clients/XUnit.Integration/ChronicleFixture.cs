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

    MongoDBDatabase? _eventStore;
    MongoDBDatabase? _eventStoreForNamespace;
    MongoDBDatabase? _readModels;
    IContainer? _container;
    INetwork? _network;
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

        // MongoDBContainer is virtual so derived fixtures can swap the container source.
        // The override is the documented extension point; the base ctor must call it to
        // trigger the lazy build-and-start cycle that every fixture relies on.
#pragma warning disable MA0056
        if (RequiresContainer)
        {
            StartContainer(MongoDBContainer).GetAwaiter().GetResult();
        }
#pragma warning restore MA0056
    }

    /// <summary>
    /// Gets the externally supplied MongoDB connection string, if configured.
    /// </summary>
    public string? ExternalMongoDBConnectionString { get; } =
        Environment.GetEnvironmentVariable("CHRONICLE_MONGODB_CONNECTION_DETAILS") is { Length: > 0 } value ? value : null;

    /// <inheritdoc/>
    public string MongoDBConnectionString => ExternalMongoDBConnectionString
        ?? $"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true";

    /// <inheritdoc/>
    public string MongoDBDatabaseNamePrefix { get; } =
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CHRONICLE_MONGODB_CONNECTION_DETAILS"))
            ? string.Empty
            : $"t_{Guid.NewGuid().ToString("N")[..16]}_";

    /// <summary>
    /// Get the MongoDB container.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when using an external MongoDB without a container.</exception>
    public virtual IContainer MongoDBContainer
    {
        get
        {
            lock (_containerLock)
            {
                if (!RequiresContainer)
                {
                    throw new InvalidOperationException("An external MongoDB connection is configured; no MongoDB container exists.");
                }

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
    public INetwork Network
    {
        get
        {
            lock (_containerLock)
            {
                return _network ??= new NetworkBuilder()
                    .WithName(Guid.NewGuid().ToString("D"))
                    .Build();
            }
        }
    }

    /// <inheritdoc/>
    public MongoDBDatabase EventStore => _eventStore ??= new(MongoDBConnectionString, $"{MongoDBDatabaseNamePrefix}{Constants.EventStoreDatabaseName}");

    /// <inheritdoc/>
    public MongoDBDatabase EventStoreForNamespace => _eventStoreForNamespace ??= new(MongoDBConnectionString, $"{MongoDBDatabaseNamePrefix}{Constants.EventStoreNamespaceDatabaseName}");

    /// <inheritdoc/>
    public MongoDBDatabase ReadModels => _readModels ??= new(MongoDBConnectionString, $"{MongoDBDatabaseNamePrefix}{Constants.ReadModelsDatabaseName}");

    /// <summary>
    /// Gets a value indicating whether this fixture must start a container.
    /// </summary>
    protected virtual bool RequiresContainer => ExternalMongoDBConnectionString is null;

    /// <summary>
    /// Gets the logger factory for creating loggers.
    /// </summary>
    protected ILoggerFactory LoggerFactory { get; }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        await (_container?.DisposeAsync() ?? ValueTask.CompletedTask);
        if (ExternalMongoDBConnectionString is not null)
        {
            _eventStore?.Dispose();
            _eventStoreForNamespace?.Dispose();
            _readModels?.Dispose();
            await DropMongoDBDatabases();
        }
        await (_network?.DisposeAsync() ?? ValueTask.CompletedTask);
    }

    /// <inheritdoc/>
    public virtual async Task PerformBackupAsync(string? prefix = null)
    {
        if (ExternalMongoDBConnectionString is not null)
        {
            return;
        }

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
    public virtual Task RemoveAllDatabases(IEnumerable<string>? excludePrefixes = null) => DropMongoDBDatabases(excludePrefixes);

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
        if (ExternalMongoDBConnectionString is not null)
        {
            return;
        }

        await MongoDBContainer.StopAsync();
        await MongoDBContainer.StartAsync();
    }

    /// <summary>
    /// Selects owned databases for cleanup, applying exclusions to their logical names.
    /// </summary>
    /// <param name="names">The available database names.</param>
    /// <param name="databaseNamePrefix">The prefix identifying databases owned by the fixture.</param>
    /// <param name="excludePrefixes">The logical database-name prefixes to preserve.</param>
    /// <returns>The database names to drop.</returns>
    internal static IEnumerable<string> GetMongoDBDatabasesToDrop(IEnumerable<string> names, string databaseNamePrefix, IEnumerable<string>? excludePrefixes = null)
    {
        var systemNames = new[] { "admin", "config", "local" };
        return names.Where(name =>
            !systemNames.Contains(name) &&
            name.StartsWith(databaseNamePrefix, StringComparison.Ordinal) &&
            excludePrefixes?.Any(p => name[databaseNamePrefix.Length..].StartsWith(p, StringComparison.OrdinalIgnoreCase)) != true);
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

    async Task DropMongoDBDatabases(IEnumerable<string>? excludePrefixes = null)
    {
        using var mongoClient = new MongoClient(MongoDBConnectionString);
        using var namesCursor = await mongoClient.ListDatabaseNamesAsync();
        var names = await namesCursor.ToListAsync();
        foreach (var name in GetMongoDBDatabasesToDrop(names, MongoDBDatabaseNamePrefix, excludePrefixes))
        {
            await mongoClient.DropDatabaseAsync(name);
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
