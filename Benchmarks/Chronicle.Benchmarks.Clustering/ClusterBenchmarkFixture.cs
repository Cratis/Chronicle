// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
extern alias KernelGrpc;
using Cratis.Arc;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.XUnit.Integration;
using Cratis.DependencyInjection;
using EphemeralMongo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost;
using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Brings up an in-process Chronicle cluster of a given <see cref="ClusterTopology"/> so the same workload
/// can be measured against one silo and against two.
/// </summary>
/// <remarks>
/// The configuration mirrors <c language="csharp">Integration/Clustering/ClusteringFixture</c> — an Orleans
/// <see cref="InProcessTestCluster"/> over an EphemeralMongo instance, with every silo co-hosting an
/// instance of the same Chronicle client application. Only the silo count and the role assignment differ
/// between topologies, so a difference in the measurement is attributable to clustering.
/// </remarks>
/// <param name="topology">The <see cref="ClusterTopology"/> to bring up.</param>
/// <param name="configureDiagnostics">Optional scenario-local service instrumentation.</param>
public sealed class ClusterBenchmarkFixture(ClusterTopology topology, Action<IServiceCollection>? configureDiagnostics = null) : IAsyncDisposable
{
    /// <summary>
    /// The name of the event store the benchmarks append to.
    /// </summary>
    public const string EventStore = "benchmarks";

    const string FirstSiloName = "Silo_0";
    static readonly TimeSpan _bringUpTimeout = TimeSpan.FromSeconds(90);

    IMongoRunner? _mongoRunner;
    InProcessTestCluster? _cluster;

    /// <summary>
    /// Gets the number of silos the topology runs.
    /// </summary>
    public int SiloCount => topology == ClusterTopology.SingleSilo ? 1 : 2;

    /// <summary>
    /// Gets the <see cref="IEventStore"/> of the client co-hosted on the first silo — the one the
    /// benchmark drives its workload through.
    /// </summary>
    public IEventStore EventStore1 => EventStores[0];

    /// <summary>
    /// Gets the <see cref="IEventStore"/> of every co-hosted client instance, one per silo.
    /// </summary>
    public IReadOnlyList<IEventStore> EventStores =>
        [.. _cluster!.Silos.Select(silo => silo.ServiceProvider.GetRequiredService<IEventStore>())];

    /// <summary>
    /// Gets the probes for whether the cluster is in a state where a measurement means anything.
    /// </summary>
    public ClusterReadiness Readiness => new(
        _cluster!.Silos[0].ServiceProvider.GetRequiredService<IGrainFactory>(),
        EventStore1,
        EventStore);

    /// <summary>
    /// Gets the MongoDB database containing the served read models.
    /// </summary>
    internal MongoDB.Driver.IMongoDatabase ReadModelDatabase =>
        _cluster!.Silos[0].ServiceProvider.GetRequiredService<Storage.MongoDB.IDatabase>()
            .GetReadModelDatabase(
                (KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName)EventStore,
                KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default);

    /// <summary>
    /// Brings the cluster up and drives one event end to end so every benchmark starts from an
    /// operational cluster rather than racing the first activation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Start()
    {
        _mongoRunner = await MongoRunner.RunAsync(new MongoRunnerOptions { UseSingleNodeReplicaSet = true });
        await BringUpCluster(_mongoRunner.ConnectionString);
        await WarmUp();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_cluster is not null)
        {
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

        _mongoRunner?.Dispose();
        _mongoRunner = null;
    }

    async Task BringUpCluster(string mongoUrl)
    {
        var builder = new InProcessTestClusterBuilder((short)SiloCount);
        builder.Options.ConfigureFileLogging = false;
        builder.Options.InitializeClientOnDeploy = false;

        builder
            .ConfigureSiloHost((siloOptions, hostBuilder) =>
            {
                hostBuilder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions()));
                hostBuilder.Logging.SetMinimumLevel(LogLevel.Warning);
                if (configureDiagnostics is not null) hostBuilder.Logging.AddConsole();

                var services = hostBuilder.Services;
                services.AddCratisMongoDB(
                    mongo =>
                    {
                        mongo.Server = mongoUrl;
                        mongo.Database = "orleans";
                    },
                    _ => { });

                // Arc TryAdds its metrics factory; register it before self-binding can install an unusable constructor binding.
                services.AddCratisArcMeter();
                services.AddTypeDiscovery();
                services.AddBindingsByConvention();
                services.AddSelfBindings();

                ConceptTypeConvertersRegistrar.EnsureFor(typeof(ClusterBenchmarkFixture).Assembly);
                ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

                var isFirstSilo = siloOptions.SiloName == FirstSiloName;
                var splitRoles = topology == ClusterTopology.TwoSilosWithSplitRoles;
                services.Configure<Configuration.ChronicleOptions>(options =>
                {
                    options.Clustering.Roles.EventSequences = !splitRoles || isFirstSilo;
                    options.Clustering.Roles.Observers = !splitRoles || !isFirstSilo;
                });
            })
            .ConfigureSilo((siloOptions, siloBuilder) =>
            {
                KernelGrpc::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                    siloBuilder,
                    chronicleBuilder => chronicleBuilder.WithMongoDB(mongoUrl, EventStore));

                siloBuilder.ConfigureServices(services =>
                {
                    KernelBootstrap.RemoveServerStartupTask(services);
                    services.AddInProcessChronicleClient(
                        new DefaultClientArtifactsProvider(new ReplayShapeAssemblyDiscovery(typeof(ClusterBenchmarkFixture).Assembly, configureDiagnostics is not null)),
                        EventStore);
                    configureDiagnostics?.Invoke(services);
                });
            });

        _cluster = builder.Build();
        await _cluster.DeployAsync();
        await WaitForActiveSilos();
        await KernelBootstrap.Run(_cluster.Silos[0].ServiceProvider, EventStore);
    }

    async Task WaitForActiveSilos()
    {
        var management = _cluster!.Silos[0].ServiceProvider.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
        using var cancellationTokenSource = new CancellationTokenSource(_bringUpTimeout);

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                var hosts = await management.GetHosts(onlyActive: true);
                if (hosts.Count(host => host.Value == SiloStatus.Active) >= SiloCount)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is OrleansException or TimeoutException)
            {
                // Membership is not ready yet — keep polling until the timeout.
            }

            await Task.Delay(500, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new InvalidOperationException($"Cluster did not reach {SiloCount} active silos within the timeout.");
    }

    async Task WarmUp()
    {
        var handlers = EventStores.Select(eventStore => eventStore.Reducers.GetHandlerFor<WarmupReducer>()).ToList();
        foreach (var handler in handlers)
        {
            await handler.WaitTillActive(_bringUpTimeout);
        }

        var appendResult = await EventStore1.EventLog.Append("cluster-warmup", new ClusterWarmedUp(1));
        await handlers[0].WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _bringUpTimeout);
    }
}
