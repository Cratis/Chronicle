// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.XUnit.Integration;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using KernelCore::Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.TestingHost;
using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Represents a fixture for clustered integration tests with 2 silos.
/// </summary>
public class ClusteringFixture : IChronicleFixture, IAsyncLifetime
{
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

    /// <summary>
    /// Gets the MongoDB connection string.
    /// </summary>
    public string MongoConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc/>
    MongoDBDatabase IChronicleFixture.EventStore => _eventStoreDatabase;

    /// <inheritdoc/>
    public MongoDBDatabase EventStoreForNamespace { get; private set; } = null!;

    /// <inheritdoc/>
    public MongoDBDatabase ReadModels { get; private set; } = null!;

    MongoDBDatabase _eventStoreDatabase = null!;

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

        MongoConnectionString = $"mongodb://localhost:{_mongoContainer.GetMappedPublicPort(27017)}/?directConnection=true";

        // Set up database helpers
        _eventStoreDatabase = new MongoDBDatabase(MongoConnectionString, "event-store");
        EventStoreForNamespace = new MongoDBDatabase(MongoConnectionString, $"event-store-{NamespaceId.Default}");
        ReadModels = new MongoDBDatabase(MongoConnectionString, "read-models");

        // Create test cluster with 2 silos
        var builder = new TestClusterBuilder(2); // 2 silos
        builder.AddSiloBuilderConfigurator<Silo1Configurator>();
        builder.AddSiloBuilderConfigurator<Silo2Configurator>();
        builder.AddClientBuilderConfigurator<ClientConfigurator>();

        // Configure both silos with shared services
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoConnectionString"] = MongoConnectionString
            });
        });

        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
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
        await _eventStoreDatabase.Drop();
        await EventStoreForNamespace.Drop();
        await ReadModels.Drop();
    }

    /// <summary>
    /// Silo 1 configurator - EventSequences only.
    /// </summary>
    class Silo1Configurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            var mongoConnectionString = siloBuilder.GetConfigurationValue<string>("MongoConnectionString")!;

            siloBuilder.Services.AddCratisArcMeter();
            siloBuilder.ConfigureServices(services =>
            {
                services.Configure<Configuration.ChronicleOptions>(options =>
                {
                    options.Clustering.EventSequences = true;
                    options.Clustering.Observers = false;
                });
            });

            KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                siloBuilder,
                chronicleBuilder => chronicleBuilder.WithMongoDB(mongoConnectionString, "integration-test"));
        }
    }

    /// <summary>
    /// Silo 2 configurator - Observers only.
    /// </summary>
    class Silo2Configurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            var mongoConnectionString = siloBuilder.GetConfigurationValue<string>("MongoConnectionString")!;

            siloBuilder.Services.AddCratisArcMeter();
            siloBuilder.ConfigureServices(services =>
            {
                services.Configure<Configuration.ChronicleOptions>(options =>
                {
                    options.Clustering.EventSequences = false;
                    options.Clustering.Observers = true;
                });
            });

            KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                siloBuilder,
                chronicleBuilder => chronicleBuilder.WithMongoDB(mongoConnectionString, "integration-test"));
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
