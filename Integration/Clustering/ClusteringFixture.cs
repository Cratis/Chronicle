// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Reflection;
using Cratis.Arc;
using Cratis.Chronicle.Integration.Clustering.for_Clustering;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using EphemeralMongo;
using Orleans.TestingHost;
using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Represents a fixture for clustered integration tests with two silos forming a single Orleans cluster.
/// </summary>
/// <remarks>
/// The cluster is an Orleans <see cref="InProcessTestCluster"/> — silos share in-memory cluster membership
/// and grain directory, while grain calls between silos still run through the full Orleans message
/// serialization pipeline (the in-memory transport sits below the regular connection/serializer stack).
/// Silo_0 hosts EventSequences (the event log grain) and additionally co-hosts the Chronicle client
/// (the <see cref="IEventStore"/> the specs talk to). Silo_1 hosts Observers (reactors, reducers and
/// projections). Because of that role split, every event and read model crosses the silo boundary —
/// exercising Orleans serialization end-to-end, which is the primary concern for clustering.
/// </remarks>
public class ClusteringFixture : IAsyncLifetime
{
    const string EventSequencesSiloName = "Silo_0";
    const string ObserversSiloName = "Silo_1";

    IMongoRunner? _mongoRunner;
    InProcessTestCluster? _cluster;

    /// <summary>
    /// Gets the <see cref="IEventStore"/> from the client co-hosted on the event-sequences silo.
    /// </summary>
    public IEventStore ClientEventStore => EventSequencesSilo.ServiceProvider.GetRequiredService<IEventStore>();

    /// <summary>
    /// Gets the <see cref="IChronicleClient"/> from the event-sequences silo.
    /// </summary>
    public IChronicleClient ChronicleClient => EventSequencesSilo.ServiceProvider.GetRequiredService<IChronicleClient>();

    /// <summary>
    /// Gets the service provider of the event-sequences silo, allowing specs to resolve silo-registered services.
    /// </summary>
    public IServiceProvider SiloServices => EventSequencesSilo.ServiceProvider;

    /// <summary>
    /// Gets the <see cref="SiloAddress"/> of the silo configured to host EventSequences grains.
    /// </summary>
    public SiloAddress EventSequencesSiloAddress => EventSequencesSilo.SiloAddress;

    /// <summary>
    /// Gets the <see cref="SiloAddress"/> of the silo configured to host observer grains.
    /// </summary>
    public SiloAddress ObserversSiloAddress => ObserversSilo.SiloAddress;

    /// <summary>
    /// Gets the shared <see cref="ClusteredReactorSignal"/> instance used by the reactor on whichever silo it runs.
    /// </summary>
    /// <remarks>
    /// Both silos share the same object reference so that a reactor handler and the test code
    /// reading from this fixture both see the same in-memory state.
    /// </remarks>
    public ClusteredReactorSignal ReactorSignal { get; } = new();

    InProcessSiloHandle EventSequencesSilo => _cluster!.Silos.Single(silo => silo.Name == EventSequencesSiloName);

    InProcessSiloHandle ObserversSilo => _cluster!.Silos.Single(silo => silo.Name == ObserversSiloName);

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _mongoRunner = await MongoRunner.RunAsync(new MongoRunnerOptions
        {
            UseSingleNodeReplicaSet = true
        });

        // The in-process test cluster removes the classic localhost-clustering races (membership gossip,
        // port contention, divergent startup ordering), but the Chronicle pipeline itself still has
        // cross-silo coordination that can occasionally land a fresh cluster in a bad state. The warmup
        // verifies the full append → cross-silo observe → reduce path end-to-end; if it fails, the cluster
        // instance is discarded and a fresh one is brought up. A bad cluster instance is cheap to discard
        // and this keeps the fixture reliable for CI.
        Exception? lastFailure = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                await BringUpClusterAsync(_mongoRunner.ConnectionString);
                await WarmUpAsync();
                return;
            }
            catch (Exception ex)
            {
                lastFailure = ex;
                Console.WriteLine($"Cluster bring-up attempt {attempt + 1} failed: {ex.Message}. Recreating cluster...");
                await TearDownClusterAsync();
            }
        }

        throw new InvalidOperationException("Failed to bring up an operational cluster after multiple attempts.", lastFailure);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await TearDownClusterAsync();
        _mongoRunner?.Dispose();
    }

    /// <summary>
    /// Builds and deploys a fresh two-silo test cluster and bootstraps the Chronicle kernel.
    /// </summary>
    /// <param name="mongoUrl">The MongoDB connection string shared by both silos.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task BringUpClusterAsync(string mongoUrl)
    {
        var builder = new InProcessTestClusterBuilder(2);
        builder.Options.ConfigureFileLogging = false;

        // The specs never use the Orleans cluster client — they talk to the co-hosted Chronicle client
        // and the silos' own service providers. The client host must not even start: ClusterClient
        // validates at startup that every type in the grain-interface manifest has a codec, and the
        // Cratis types are only serializable on hosts that run Chronicle's serialization configuration
        // (part of AddChronicleToSilo), which a plain client host does not.
        builder.Options.InitializeClientOnDeploy = false;

        builder
            .ConfigureSiloHost((siloOptions, hostBuilder) =>
            {
                // The testing host builds each silo host with the Development environment name, which turns
                // on ValidateScopes/ValidateOnBuild. The Chronicle server never runs as Development, so its
                // registrations are not shaped for eager build-time container validation — swap in a
                // non-validating container factory to match how the server actually runs.
                hostBuilder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions()));

                hostBuilder.Logging.AddConsole();
                hostBuilder.Logging.SetMinimumLevel(LogLevel.Warning);

                var services = hostBuilder.Services;
                services.AddCratisMongoDB(
                    mongo =>
                    {
                        mongo.Server = mongoUrl;
                        mongo.Database = "orleans";
                    },
                    _ => { });

                services.AddTypeDiscovery();
                services.AddBindingsByConvention();
                services.AddSelfBindings();
                services.AddCratisArcMeter();
                services.AddSingleton(ReactorSignal);

                ConceptTypeConvertersRegistrar.EnsureFor(typeof(ClusteringFixture).Assembly);
                ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

                // Role split: Silo_0 owns EventSequences (the event log grain), Silo_1 owns Observers
                // (reactors, reducers, projections). This forces every event and read model to cross
                // the silo boundary — exercising Orleans serialization end-to-end.
                var isEventSequencesSilo = siloOptions.SiloName == EventSequencesSiloName;
                services.Configure<Configuration.ChronicleOptions>(options =>
                {
                    options.Clustering.Roles.EventSequences = isEventSequencesSilo;
                    options.Clustering.Roles.Observers = !isEventSequencesSilo;
                });
            })
            .ConfigureSilo((siloOptions, siloBuilder) =>
            {
                KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                    siloBuilder,
                    chronicleBuilder => chronicleBuilder.WithMongoDB(mongoUrl, Constants.EventStore));

                siloBuilder.ConfigureServices(services =>
                {
                    RemoveChronicleServerStartupTask(services);
                    if (siloOptions.SiloName == EventSequencesSiloName)
                    {
                        services.AddInProcessChronicleClient(
                            new DefaultClientArtifactsProvider(new SingleAssemblyDiscovery(typeof(ClusteringFixture).Assembly)),
                            Constants.EventStore);
                    }
                });
            });

        _cluster = builder.Build();
        await _cluster.DeployAsync();

        // With InitializeClientOnDeploy off, DeployAsync skips its own stabilization wait (it polls
        // through the cluster client), so wait for membership to reach two active silos here before
        // any grain placement happens.
        await WaitForActiveSilos(expectedSilos: 2);

        // Manually perform the kernel bootstrap that ChronicleServerStartupTask normally handles.
        // That task is removed because it activates grains during silo startup, before the cluster is
        // fully formed — with role-based placement that either deadlocks or fails placement. Now that
        // both silos are deployed and membership has stabilized, all grain activations succeed.
        await BootstrapKernelAsync();
    }

    /// <summary>
    /// Manually bootstraps the Chronicle kernel after the cluster is fully formed.
    /// </summary>
    /// <remarks>
    /// Equivalent to the subset of <c>ChronicleServerStartupTask</c> that the warmup requires:
    /// system namespace creation, system reactor registration (so <c>EventStoreAdded</c> events are
    /// handled), and user event store namespace creation + reactor registration.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task BootstrapKernelAsync()
    {
        var services = EventSequencesSilo.ServiceProvider;
        var grainFactory = services.GetRequiredService<IGrainFactory>();
        var kernelReactors = services.GetRequiredService<KernelCore::Cratis.Chronicle.Observation.Reactors.Kernel.IReactors>();

        var systemEventStore = (string)KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName.System;
        var userEventStore = (string)(KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName)Constants.EventStore;

        await grainFactory.GetGrain<KernelCore::Cratis.Chronicle.Namespaces.INamespaces>(systemEventStore).EnsureDefault();
        await kernelReactors.DiscoverAndRegister(
            KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName.System,
            KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default);

        await grainFactory.GetGrain<KernelCore::Cratis.Chronicle.Namespaces.INamespaces>(userEventStore).EnsureDefault();
        await kernelReactors.DiscoverAndRegister(
            (KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName)Constants.EventStore,
            KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default);
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
    /// <param name="expectedSilos">The number of active silos to wait for.</param>
    /// <returns>A <see cref="Task"/> that completes when the cluster has converged.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the cluster does not converge within the timeout.</exception>
    async Task WaitForActiveSilos(int expectedSilos)
    {
        var management = EventSequencesSilo.ServiceProvider.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
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

    /// <summary>
    /// Stops and disposes the test cluster so a fresh one can be brought up. The shared MongoDB instance is left running.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task TearDownClusterAsync()
    {
        if (_cluster is null)
        {
            return;
        }

        try
        {
            await _cluster.StopAllSilosAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ignoring error while stopping silos during teardown: {ex.Message}");
        }

        await _cluster.DisposeAsync();
        _cluster = null;
    }

    /// <summary>
    /// Removes the <c>ChronicleServerStartupTask</c> which activates grains during silo startup, before
    /// the cluster is formed. The fixture drives the equivalent bootstrap itself once the cluster is up.
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
