// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Arc;
using Cratis.Chronicle.Jobs;
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
using IKernelObserver = KernelCore::Cratis.Chronicle.Observation.IObserver;
using KernelEventSequenceId = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using KernelEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using KernelEventStoreNamespaceName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using ObserverKey = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverKey;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Brings up an in-process Chronicle cluster of a given <see cref="ClusterTopology"/> so the same workload
/// can be measured against one silo and against two.
/// </summary>
/// <remarks>
/// The configuration mirrors <c>Integration/Clustering/ClusteringFixture</c> — an Orleans
/// <see cref="InProcessTestCluster"/> over an EphemeralMongo instance, with every silo co-hosting an
/// instance of the same Chronicle client application. Only the silo count and the role assignment differ
/// between topologies, so a difference in the measurement is attributable to clustering.
/// </remarks>
/// <param name="topology">The <see cref="ClusterTopology"/> to bring up.</param>
public sealed class ClusterBenchmarkFixture(ClusterTopology topology) : IAsyncDisposable
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

    /// <summary>
    /// Waits until the kernel observer with the given identifier is subscribed to at least one event type,
    /// and throws when it never gets there.
    /// </summary>
    /// <remarks>
    /// An observer that is not subscribed never does any work, so a wait for it to reach a sequence number
    /// would either return immediately or time out — either way the benchmark would be measuring the append
    /// alone. This makes that state fail loudly before the measured window opens. The subscription's target
    /// list is deliberately not checked: it only holds connected client instances, and kernel-owned
    /// subscriptions such as projections legitimately have none.
    /// </remarks>
    /// <param name="observerId">The identifier of the observer.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the observer never becomes subscribed.</exception>
    public async Task WaitForSubscribedObserver(string observerId, TimeSpan timeout)
    {
        var observer = _cluster!.Silos[0].ServiceProvider.GetRequiredService<IGrainFactory>()
            .GetGrain<IKernelObserver>(new ObserverKey(
                observerId,
                (KernelEventStoreName)EventStore,
                KernelEventStoreNamespaceName.Default,
                KernelEventSequenceId.Log));

        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var subscription = await observer.GetSubscription();
            if (subscription.IsSubscribed && subscription.EventTypes.Any())
            {
                return;
            }

            await Task.Delay(200, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new InvalidOperationException(
            $"Observer '{observerId}' never became subscribed to any event type. Measuring it would not include any observer work.");
    }

    /// <summary>
    /// Waits until no job in the event store is preparing or running.
    /// </summary>
    /// <remarks>
    /// Observer work is driven by jobs, so this is the cluster's "nothing in flight" signal. Used both to
    /// open a measured window from a quiescent cluster and to close one only once the work it triggered —
    /// including any catch-up the replay leaves behind — has actually finished.
    /// </remarks>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when jobs are still in flight after the timeout.</exception>
    public async Task WaitForNoJobsInFlight(TimeSpan timeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var jobs = await EventStore1.Jobs.GetJobs();
            if (!jobs.Any(job => job.Status is JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps or JobStatus.Running or JobStatus.Removing))
            {
                return;
            }

            await Task.Delay(50, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        throw new InvalidOperationException("Jobs were still in flight after the timeout.");
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
                KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                    siloBuilder,
                    chronicleBuilder => chronicleBuilder.WithMongoDB(mongoUrl, EventStore));

                siloBuilder.ConfigureServices(services =>
                {
                    KernelBootstrap.RemoveServerStartupTask(services);
                    services.AddInProcessChronicleClient(
                        new DefaultClientArtifactsProvider(new BenchmarkAssemblyDiscovery(typeof(ClusterBenchmarkFixture).Assembly)),
                        EventStore);
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
