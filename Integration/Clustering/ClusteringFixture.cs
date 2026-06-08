// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Arc;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.XUnit.Integration;
using Cratis.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Represents a fixture for clustered integration tests with 2 silos.
/// </summary>
public class ClusteringFixture : IChronicleFixture, IAsyncLifetime
{
    readonly INetwork _network;
    IContainer? _mongoContainer;
    IHost? _silo1;
    IHost? _silo2;

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
    /// Gets the <see cref="IEventStore"/> instance from Silo1.
    /// </summary>
    public IEventStore EventStore => _silo1!.Services.GetRequiredService<IEventStore>();

    /// <summary>
    /// Gets the <see cref="IChronicleClient"/> instance from Silo1.
    /// </summary>
    public IChronicleClient ChronicleClient => _silo1!.Services.GetRequiredService<IChronicleClient>();

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

        var mongoUrl = $"mongodb://{_mongoContainer.Hostname}:{_mongoContainer.GetMappedPublicPort(27017)}/";

        // Create Silo 1 (EventSequences only)
        _silo1 = CreateSilo(
            siloName: "silo1",
            siloPort: 11111,
            gatewayPort: 30000,
            mongoUrl: mongoUrl,
            eventSequences: true,
            observers: false);
        await _silo1.StartAsync();

        // Create Silo 2 (EventSequences + Observers) 
        _silo2 = CreateSilo(
            siloName: "silo2",
            siloPort: 11112,
            gatewayPort: 30001,
            mongoUrl: mongoUrl,
            eventSequences: true,
            observers: true);
        await _silo2.StartAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_silo1 is not null)
        {
            await _silo1.StopAsync();
            _silo1.Dispose();
        }

        if (_silo2 is not null)
        {
            await _silo2.StopAsync();
            _silo2.Dispose();
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

    IHost CreateSilo(string siloName, int siloPort, int gatewayPort, string mongoUrl, bool eventSequences, bool observers)
    {
        var builder = Host.CreateDefaultBuilder();

        builder.AddCratisMongoDB(
            mongo =>
            {
                mongo.Server = mongoUrl;
                mongo.Database = "orleans";
                mongo.DirectConnection = true;
            },
            _ => { });

        builder
            .ConfigureServices((ctx, services) =>
            {
                services.AddTypeDiscovery();
                services.AddBindingsByConvention();
                services.AddSelfBindings();
                services.AddCratisArcMeter();

                // Register concept type converters
                ConceptTypeConvertersRegistrar.EnsureFor(typeof(ClusteringFixture).Assembly);
                ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

                services.Configure<Configuration.ChronicleOptions>(options =>
                {
                    options.Clustering.Roles.EventSequences = eventSequences;
                    options.Clustering.Roles.Observers = observers;
                });
            });

        builder.UseOrleans((ctx, siloBuilder) =>
        {
            siloBuilder
                .UseLocalhostClustering(siloPort, gatewayPort);

            if (_mongoContainer is not null)
            {
                KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                    siloBuilder,
                    chronicleBuilder => chronicleBuilder.WithMongoDB(mongoUrl, "integration-test"));
            }
        });

        return builder.Build();
    }
}
