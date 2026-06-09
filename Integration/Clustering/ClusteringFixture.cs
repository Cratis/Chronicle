// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Cratis.Arc;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Driver;
using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Represents a fixture for clustered integration tests with two silos forming a single Orleans cluster.
/// </summary>
/// <remarks>
/// Silo1 is the primary silo and additionally co-hosts the Chronicle client (the <see cref="IEventStore"/>
/// the specs talk to). Silo2 is a secondary silo joined to the same cluster. Because grains (event sequences,
/// observers, reducers and projections) are placed across both silos, every event and read model crosses
/// the silo boundary — exercising Orleans serialization end-to-end, which is the primary concern for clustering.
/// </remarks>
public class ClusteringFixture : IChronicleFixture, IAsyncLifetime
{
    IContainer? _mongoContainer;
    IHost? _silo1;
    IHost? _silo2;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusteringFixture"/> class.
    /// </summary>
    public ClusteringFixture()
    {
        Network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
    }

    /// <inheritdoc/>
    public INetwork Network { get; }

    /// <inheritdoc/>
    public IContainer MongoDBContainer => _mongoContainer!;

    /// <inheritdoc/>
    public MongoDBDatabase EventStore => new(_mongoContainer!, Constants.EventStoreDatabaseName);

    /// <inheritdoc/>
    public MongoDBDatabase EventStoreForNamespace => new(_mongoContainer!, Constants.EventStoreNamespaceDatabaseName);

    /// <inheritdoc/>
    public MongoDBDatabase ReadModels => new(_mongoContainer!, Constants.ReadModelsDatabaseName);

    /// <summary>
    /// Gets the <see cref="IEventStore"/> from the client co-hosted on the primary silo.
    /// </summary>
    public IEventStore ClientEventStore => _silo1!.Services.GetRequiredService<IEventStore>();

    /// <summary>
    /// Gets the <see cref="IChronicleClient"/> from the primary silo.
    /// </summary>
    public IChronicleClient ChronicleClient => _silo1!.Services.GetRequiredService<IChronicleClient>();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await Network.CreateAsync();

        var mongoBuilder = new ContainerBuilder("mongo")
            .WithCommand("/bin/sh", "-c", "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; mongosh --eval 'rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]})' || true; tail -f /dev/null")
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithHostname("mongo")
            .WithNetwork(Network)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(27017)
                .UntilCommandIsCompleted("/bin/sh", "-c", "mongosh --quiet --eval 'rs.status().ok' | grep -q 1"));

        _mongoContainer = mongoBuilder.Build();
        await _mongoContainer.StartAsync();

        var mongoUrl = $"mongodb://{_mongoContainer.Hostname}:{_mongoContainer.GetMappedPublicPort(27017)}/";

        // Localhost multi-silo clustering is inherently racy: occasionally a freshly-formed cluster lands
        // grains in a state where cross-silo observer activation never completes. Rather than fight every
        // race individually, bring up the whole cluster and verify it end-to-end with a warmup; if that
        // fails, tear the silos down and bring up a completely fresh cluster on new ports. A bad cluster
        // instance is cheap to discard and this makes the fixture reliable for CI.
        Exception? lastFailure = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                await BringUpClusterAsync(mongoUrl);
                await WarmUpAsync();
                return;
            }
            catch (Exception ex)
            {
                lastFailure = ex;
                Console.WriteLine($"Cluster bring-up attempt {attempt + 1} failed: {ex.Message}. Recreating cluster...");
                await TearDownSilosAsync();
            }
        }

        throw new InvalidOperationException("Failed to bring up an operational cluster after multiple attempts.", lastFailure);
    }

    /// <summary>
    /// Builds and starts a fresh two-silo cluster and waits for membership to converge.
    /// </summary>
    /// <param name="mongoUrl">The MongoDB connection string shared by both silos.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task BringUpClusterAsync(string mongoUrl)
    {
        var silo1Port = GetFreePort();
        var silo1Gateway = GetFreePort();
        var silo2Port = GetFreePort();
        var silo2Gateway = GetFreePort();
        var primaryEndpoint = new IPEndPoint(IPAddress.Loopback, silo1Port);

        // Build BOTH hosts before starting either. Orleans builds its serializer type manifest
        // (and the well-known type id assignments) from the set of assemblies loaded at host-build
        // time. Starting silo1 first would JIT-load additional assemblies, so building silo2
        // afterwards would produce a divergent manifest and cross-silo deserialization would fail
        // with "well-known type id not present". Building both back-to-back keeps the manifests
        // identical across the cluster.
        _silo1 = CreateSilo(silo1Port, silo1Gateway, primaryEndpoint, mongoUrl, hostClient: true);
        _silo2 = CreateSilo(silo2Port, silo2Gateway, primaryEndpoint, mongoUrl, hostClient: false);

        // Silo1 is the primary and must be up and visible in membership before the secondary joins,
        // otherwise the secondary can fail to gossip and the cluster never converges.
        await StartSilo(_silo1, "silo1");
        await WaitForActiveSilos(_silo1, expectedSilos: 1);

        await StartSilo(_silo2, "silo2");

        // Wait for membership to actually converge to two active silos rather than relying on a fixed
        // delay — localhost multi-silo clustering is otherwise racy and grain placement can hang.
        await WaitForActiveSilos(_silo1, expectedSilos: 2);
    }

    /// <summary>
    /// Stops and disposes both silos so a fresh cluster can be brought up. The shared MongoDB container is left running.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task TearDownSilosAsync()
    {
        foreach (var silo in new[] { _silo2, _silo1 })
        {
            if (silo is null)
            {
                continue;
            }

            try
            {
                await silo.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ignoring error while stopping silo during teardown: {ex.Message}");
            }

            silo.Dispose();
        }

        _silo1 = null;
        _silo2 = null;
    }

    /// <summary>
    /// Drives one warmup event through the full pipeline (client connect → artifact registration →
    /// cross-silo observer activation → reduce) before any test runs. Establishing the client connection
    /// and activating observers is the racy part of clustered startup; doing it here deterministically means
    /// each test finds a fully operational cluster instead of racing the first activation. Any failure
    /// propagates to the caller, which discards this cluster instance and brings up a fresh one.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task WarmUpAsync()
    {
        var warmupTimeout = TimeSpan.FromSeconds(40);

        // Resolving the event store establishes the in-process client connection and registers every
        // artifact in the assembly. Driving one event end-to-end through a dedicated warmup reducer confirms
        // the full append → cross-silo observe → reduce path is operational before any test runs; if it is
        // not, the caller discards this cluster and brings up a fresh one.
        var eventStore = ClientEventStore;
        var reducerHandler = eventStore.Reducers.GetHandlerFor<ClusterWarmupReducer>();
        await reducerHandler.WaitTillActive(warmupTimeout);

        var appendResult = await eventStore.EventLog.Append("cluster-warmup", new ClusterWarmedUp(1));
        await reducerHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, warmupTimeout);
    }

    /// <summary>
    /// Polls cluster membership until the expected number of silos are active, so that grains can be
    /// placed across the cluster before any test runs.
    /// </summary>
    /// <param name="silo">A silo whose grain factory is used to query membership.</param>
    /// <param name="expectedSilos">The number of active silos to wait for.</param>
    /// <returns>A <see cref="Task"/> that completes when the cluster has converged.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the cluster does not converge within the timeout.</exception>
    static async Task WaitForActiveSilos(IHost silo, int expectedSilos)
    {
        var management = silo.Services.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var hosts = await management.GetHosts(onlyActive: true);
                if (hosts.Count(_ => _.Value == SiloStatus.Active) >= expectedSilos)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is OrleansException or TimeoutException)
            {
                // Membership not ready yet — keep polling until the timeout.
            }

            await Task.Delay(500, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new InvalidOperationException($"Cluster did not reach {expectedSilos} active silos within the timeout.");
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_silo2 is not null)
        {
            await _silo2.StopAsync();
            _silo2.Dispose();
        }

        if (_silo1 is not null)
        {
            await _silo1.StopAsync();
            _silo1.Dispose();
        }

        if (_mongoContainer is not null)
        {
            await _mongoContainer.DisposeAsync();
        }

        await Network.DisposeAsync();
    }

    /// <inheritdoc/>
    ValueTask IAsyncDisposable.DisposeAsync() => new(DisposeAsync());

    /// <inheritdoc/>
    public Task PerformBackupAsync(string? prefix = null) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task RemoveAllDatabases(IEnumerable<string>? excludePrefixes = null)
    {
        if (_mongoContainer is null) return;

        var urlBuilder = new MongoUrlBuilder($"mongodb://localhost:{_mongoContainer.GetMappedPublicPort(27017)}")
        {
            DirectConnection = true
        };
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
        using var mongoClient = new MongoClient(settings);
        var namesCursor = await mongoClient.ListDatabaseNamesAsync();
        var names = await namesCursor.ToListAsync<string>();
        var systemNames = new[] { "admin", "config", "local", "orleans" };
        foreach (var name in names.Where(name =>
            !systemNames.Contains(name) &&
            excludePrefixes?.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)) != true))
        {
            await mongoClient.DropDatabaseAsync(name);
        }
    }

    static async Task StartSilo(IHost silo, string name)
    {
        try
        {
            await silo.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start {name}: {ex}");
            throw;
        }
    }

    static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    IHost CreateSilo(int siloPort, int gatewayPort, IPEndPoint primaryEndpoint, string mongoUrl, bool hostClient)
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

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
        });

        builder
            .UseDefaultServiceProvider(_ => _.ValidateOnBuild = false)
            .ConfigureServices((ctx, services) =>
            {
                services.AddTypeDiscovery();
                services.AddBindingsByConvention();
                services.AddSelfBindings();
                services.AddCratisArcMeter();

                ConceptTypeConvertersRegistrar.EnsureFor(typeof(ClusteringFixture).Assembly);
                ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

                services.Configure<Configuration.ChronicleOptions>(options =>
                {
                    options.Clustering.Roles.EventSequences = true;
                    options.Clustering.Roles.Observers = true;
                });
            });

        builder.UseOrleans((ctx, siloBuilder) =>
        {
            siloBuilder.UseLocalhostClustering(
                siloPort,
                gatewayPort,
                primaryEndpoint,
                serviceId: "clustering-test",
                clusterId: "clustering-test");

            siloBuilder.Services.AddTypeDiscovery();
            siloBuilder.Services.AddBindingsByConvention();
            siloBuilder.Services.AddSelfBindings();

            KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                siloBuilder,
                chronicleBuilder => chronicleBuilder.WithMongoDB(mongoUrl, Constants.EventStore));

            siloBuilder.AddActivityPropagation();

            siloBuilder.ConfigureServices(services =>
            {
                RemoveChronicleServerStartupTask(services);
                if (hostClient)
                {
                    services.AddInProcessChronicleClient(
                        new DefaultClientArtifactsProvider(new SingleAssemblyDiscovery(typeof(ClusteringFixture).Assembly)),
                        Constants.EventStore);
                }
            });
        });

        return builder.Build();
    }

    /// <summary>
    /// Removes the <c>ChronicleServerStartupTask</c> which deadlocks during in-process test silo startup.
    /// Tests drive their own bootstrapping through the co-hosted client.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to adjust.</param>
    static void RemoveChronicleServerStartupTask(IServiceCollection services)
    {
        var startupTaskType = typeof(KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions).Assembly
            .GetType("Orleans.Hosting.ChronicleServerStartupTask");
        if (startupTaskType is not null)
        {
            foreach (var descriptor in services.Where(d => d.ImplementationType == startupTaskType).ToList())
            {
                services.Remove(descriptor);
            }
        }
    }

    /// <summary>
    /// Provides type discovery scoped to a single assembly — the clustering test assembly.
    /// </summary>
    /// <param name="assembly">The assembly to discover types from.</param>
    sealed class SingleAssemblyDiscovery(Assembly assembly) : ICanProvideAssembliesForDiscovery
    {
        public IEnumerable<Assembly> Assemblies => [assembly];

        public IEnumerable<Type> DefinedTypes => assembly.DefinedTypes.Select(_ => _.AsType());

        public void Initialize()
        {
        }
    }
}
