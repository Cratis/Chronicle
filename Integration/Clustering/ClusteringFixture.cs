// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.XUnit.Integration;
using Cratis.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Represents a fixture for clustered integration tests with 2 silos.
/// </summary>
public class ClusteringFixture : IChronicleFixture, IAsyncLifetime
{
    /// <summary>
    /// Static reference to allow configurators to access mongo container during silo initialization.
    /// </summary>
    static IContainer? _mongoContainerStatic;

    readonly INetwork _network;
    IContainer? _mongoContainer;
    TestCluster? _cluster;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusteringFixture"/> class.
    /// </summary>
    public ClusteringFixture()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
    }

    /// <inheritdoc/>
    public INetwork Network => _network;

    /// <inheritdoc/>
    public IContainer MongoDBContainer => _mongoContainer!;

    /// <inheritdoc/>
    MongoDBDatabase IChronicleFixture.EventStore => new(_mongoContainer!, "event-store");

    /// <inheritdoc/>
    public MongoDBDatabase EventStoreForNamespace => new(_mongoContainer!, "event-store-default");

    /// <inheritdoc/>
    public MongoDBDatabase ReadModels => new(_mongoContainer!, "read-models");

    /// <summary>
    /// Gets the Orleans test cluster.
    /// </summary>
    public TestCluster Cluster => _cluster!;

    /// <summary>
    /// Gets the <see cref="IEventStore"/> instance.
    /// </summary>
    public IEventStore EventStore => Cluster.ServiceProvider.GetRequiredService<IEventStore>();

    /// <summary>
    /// Gets the <see cref="IChronicleClient"/> instance.
    /// </summary>
    public IChronicleClient ChronicleClient => Cluster.ServiceProvider.GetRequiredService<IChronicleClient>();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _network.CreateAsync();

        // Start MongoDB container
        var mongoBuilder = new ContainerBuilder("mongo")
            .WithCommand("/bin/sh", "-c", "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; mongosh --eval 'rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]})' || true; tail -f /dev/null")
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithHostname("mongo")
            .WithNetwork(_network)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(27017)
                .UntilCommandIsCompleted("/bin/sh", "-c", "mongosh --quiet --eval 'rs.status().ok' | grep -q 1"));

        _mongoContainer = mongoBuilder.Build();
        await _mongoContainer.StartAsync();

        // Set static reference for configurators
        _mongoContainerStatic = _mongoContainer;

        // Create test cluster with 2 silos
        var builder = new TestClusterBuilder(2)
            .AddSiloBuilderConfigurator<Silo1Configurator>()
            .AddSiloBuilderConfigurator<Silo2Configurator>()
            .AddClientBuilderConfigurator<ClientConfigurator>();

        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        _mongoContainerStatic = null;

        if (_cluster is not null)
        {
            await _cluster.StopAllSilosAsync();
            await _cluster.DisposeAsync();
        }

        if (_mongoContainer is not null)
        {
            await _mongoContainer.DisposeAsync();
        }

        await _network.DisposeAsync();
    }

    /// <inheritdoc/>
    ValueTask IAsyncDisposable.DisposeAsync() => new(DisposeAsync());

    /// <inheritdoc/>
    public Task PerformBackupAsync(string? prefix = null)
    {
        // Backup not implemented for clustering tests
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task RemoveAllDatabases(IEnumerable<string>? excludePrefixes = null)
    {
        if (_mongoContainer is null) return;

        var eventStoreDb = new MongoDBDatabase(_mongoContainer, "event-store");
        var eventStoreForNamespaceDb = new MongoDBDatabase(_mongoContainer, "event-store-default");
        var readModelsDb = new MongoDBDatabase(_mongoContainer, "read-models");

        try
        {
            await eventStoreDb.Database.Client.DropDatabaseAsync("event-store");
        }
        catch
        {
            // Ignore errors if database doesn't exist
        }

        try
        {
            await eventStoreForNamespaceDb.Database.Client.DropDatabaseAsync("event-store-default");
        }
        catch
        {
            // Ignore errors if database doesn't exist
        }

        try
        {
            await readModelsDb.Database.Client.DropDatabaseAsync("read-models");
        }
        catch
        {
            // Ignore errors if database doesn't exist
        }

        eventStoreDb.Dispose();
        eventStoreForNamespaceDb.Dispose();
        readModelsDb.Dispose();
    }

    /// <summary>
    /// Silo 1 configurator - EventSequences only.
    /// </summary>
    class Silo1Configurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Services.AddTypeDiscovery();
            siloBuilder.Services.AddBindingsByConvention();
            siloBuilder.Services.AddSelfBindings();

            // Register concept type converters after initial setup
            ConceptTypeConvertersRegistrar.EnsureFor(typeof(ClusteringFixture).Assembly);
            ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

            siloBuilder.ConfigureServices(services =>
            {
                services.Configure<Configuration.ChronicleOptions>(options =>
                {
                    options.Clustering.Roles.EventSequences = true;
                    options.Clustering.Roles.Observers = false;
                });
            });

            if (_mongoContainerStatic is not null)
            {
                var mongoUrl = $"mongodb://{_mongoContainerStatic.Hostname}:{_mongoContainerStatic.GetMappedPublicPort(27017)}/";
                KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                    siloBuilder,
                    builder => builder.WithMongoDB(mongoUrl, "integration-test"));
            }
        }
    }

    /// <summary>
    /// Silo 2 configurator - Observers only.
    /// </summary>
    class Silo2Configurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Services.AddTypeDiscovery();
            siloBuilder.Services.AddBindingsByConvention();
            siloBuilder.Services.AddSelfBindings();

            // Register concept type converters after initial setup
            ConceptTypeConvertersRegistrar.EnsureFor(typeof(ClusteringFixture).Assembly);
            ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

            siloBuilder.ConfigureServices(services =>
            {
                services.Configure<Configuration.ChronicleOptions>(options =>
                {
                    options.Clustering.Roles.EventSequences = false;
                    options.Clustering.Roles.Observers = true;
                });
            });

            if (_mongoContainerStatic is not null)
            {
                var mongoUrl = $"mongodb://{_mongoContainerStatic.Hostname}:{_mongoContainerStatic.GetMappedPublicPort(27017)}/";
                KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                    siloBuilder,
                    builder => builder.WithMongoDB(mongoUrl, "integration-test"));
            }
        }
    }

    /// <summary>
    /// Client configurator.
    /// </summary>
    class ClientConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            // Client configuration if needed
        }
    }
}
