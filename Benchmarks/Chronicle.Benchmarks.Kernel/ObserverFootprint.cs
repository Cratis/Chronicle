// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Measures how much live heap one observer grain activation retains, in an in-process silo with in-memory storage.
/// </summary>
/// <remarks>
/// BenchmarkDotNet measures allocations of a call; what matters here is what stays resident between calls. The harness
/// activates a number of observers, forces full blocking collections and reports the growth of the live heap divided
/// by the number of activations. It then unsubscribes them, lets the idle deactivation (when enabled) release them and
/// measures again. Run it with <c language="bash">--observer-footprint</c>; see the README.
/// <para>
/// Every observer is served by one client connection registered with the silo before the baseline, so the watchdog
/// keeps the subscriptions instead of dropping them for a client it does not know. The warmup observers stay
/// subscribed for the whole run: they are part of the baseline and of both measurements, and the idle deactivation
/// cannot release them out from under the baseline.
/// </para>
/// </remarks>
public static class ObserverFootprint
{
    /// <summary>
    /// The event store the measured observers belong to.
    /// </summary>
    internal const string EventStore = "footprint";

    /// <summary>
    /// The namespace the measured observers belong to.
    /// </summary>
    internal const string Namespace = "Default";

    /// <summary>
    /// The kernel startup task the harness keeps out of the silo.
    /// </summary>
    internal const string StartupTaskTypeName = "Orleans.Hosting.ChronicleServerStartupTask";

    /// <summary>
    /// The client connection the measured observers are subscribed for.
    /// </summary>
    internal static readonly ConnectionId Connection = new("footprint-connection");

    /// <summary>
    /// Runs the measurement.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Run(string[] args)
    {
        var count = IntArgument(args, "--count", 2000);
        var idleSeconds = IntArgument(args, "--idle-deactivation", 0);
        var collectionAgeSeconds = IntArgument(args, "--collection-age", 0);
        var warmup = Math.Min(200, Math.Max(count / 10, 10));

        using var host = BuildHost(idleSeconds, collectionAgeSeconds);
        await host.StartAsync();
        var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
        var siloAddress = host.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;
        await ConnectClient(host);

        Console.WriteLine($"Observer activation footprint: {count} observers, idle deactivation after {idleSeconds} s (0 = disabled), Orleans collection age {(collectionAgeSeconds > 0 ? $"{collectionAgeSeconds} s" : "default")}");

        // Warm up JIT, type caches and shared singletons so they are not attributed to the measured activations. The
        // warmup observers go through the unsubscribe path too, and are then subscribed again so that they stay resident.
        await Activate(grainFactory, siloAddress, "warmup", warmup);
        await Unsubscribe(grainFactory, "warmup", warmup);
        await Activate(grainFactory, siloAddress, "warmup", warmup);

        // With a short collection age Orleans collects the warmup observers' companion grains during the run. Let it do
        // so before the baseline, or the unsubscribed phase would subtract them from the measured observers.
        if (collectionAgeSeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds((collectionAgeSeconds * 2) + 5));
        }

        var baseline = LiveHeap();
        var baselineActivations = await ActivationsOf<Observer>(grainFactory);
        Console.WriteLine($"baseline: live heap {baseline / 1024.0 / 1024.0:F1} MiB, observer activations: {baselineActivations}, companion observer alert activations: {await ActivationsOf<Observation.Alerts.ObserverAlerts>(grainFactory)}");

        var stopwatch = Stopwatch.StartNew();
        await Activate(grainFactory, siloAddress, "measured", count);
        stopwatch.Stop();
        var subscribed = LiveHeap();
        await Report(grainFactory, "subscribed", baseline, subscribed, baselineActivations, count);
        Console.WriteLine($"  subscribed when measured: {await CountSubscribed(grainFactory, "measured", count)} of {count} measured observers");
        Console.WriteLine($"  activation time: {stopwatch.Elapsed.TotalMilliseconds / count:F2} ms per observer");
        await Hold(IntArgument(args, "--hold", 0));

        await Unsubscribe(grainFactory, "measured", count);

        // An observer gives its alert reminder up on the first tick, which Orleans spreads over the first one to two
        // minutes, and only then counts as idle. Twice the periods plus a few watchdog ticks covers the rest.
        var periods = (Math.Max(idleSeconds, collectionAgeSeconds) * 2) + 5;
        var settleSeconds = IntArgument(args, "--settle", idleSeconds > 0 ? 120 + periods : periods);
        Console.WriteLine($"  waiting {settleSeconds} s for idle observers to be released");
        await Task.Delay(TimeSpan.FromSeconds(settleSeconds));

        var idle = LiveHeap();
        await Report(grainFactory, "unsubscribed", baseline, idle, baselineActivations, count);

        // Asking a released observer would activate it again, so only the warmup observers, which stay resident, are asked.
        Console.WriteLine($"  warmup observers still subscribed: {await CountSubscribed(grainFactory, "warmup", warmup)} of {warmup}");
        await Hold(IntArgument(args, "--hold-unsubscribed", 0));

        await host.StopAsync();
        return 0;
    }

    /// <summary>
    /// Builds the in-process silo the measurements run against: in-memory storage, no kernel startup task.
    /// </summary>
    /// <param name="idleSeconds">Seconds before an idle unsubscribed observer is released; 0 disables it.</param>
    /// <param name="collectionAgeSeconds">Orleans collection age in seconds; 0 keeps the Orleans default.</param>
    /// <param name="configure">Optional extra configuration of the silo.</param>
    /// <returns>The host, not started.</returns>
    internal static IHost BuildHost(int idleSeconds, int collectionAgeSeconds, Action<ISiloBuilder>? configure = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{ChronicleOptions.SectionPath}:Storage:Type"] = StorageType.InMemory,
            [$"{ChronicleOptions.SectionPath}:Observers:WatchdogInterval"] = "1",
            [$"{ChronicleOptions.SectionPath}:Observers:IdleDeactivationAfter"] = idleSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),

            // The footprint client never pings, so the connected clients registry must not expire it during the run.
            [$"{ChronicleOptions.SectionPath}:ConnectedClients:StaleThresholdSeconds"] = "86400",
        });
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(serviceId: "footprint", clusterId: "footprint");
            if (collectionAgeSeconds > 0)
            {
                // Lets Orleans collect idle grains without [KeepAlive], such as the observer alert companions, within the run.
                silo.Configure<GrainCollectionOptions>(options =>
                {
                    options.CollectionAge = TimeSpan.FromSeconds(collectionAgeSeconds);
                    options.CollectionQuantum = TimeSpan.FromSeconds(1);
                });
            }

            configure?.Invoke(silo);
            silo.Services.AddTypeDiscovery();
            silo.Services.AddBindingsByConvention();
            silo.Services.AddSelfBindings();
            silo.AddChronicleToSilo(chronicle => chronicle.WithInMemory());
            silo.ConfigureServices(services =>
            {
                services.AddOptions<ChronicleOptions>().BindConfiguration(ChronicleOptions.SectionPath);

                // The startup task provisions system event stores and registers kernel artifacts; none of it is part
                // of what an observer activation retains, and it would add its own resident grains to the measurement.
                var startupTaskType = typeof(ChronicleOptions).Assembly.GetType(StartupTaskTypeName)
                    ?? throw new InvalidOperationException($"The startup task '{StartupTaskTypeName}' was not found; it was renamed or moved, so the measurement would include kernel artifacts");
                var startupTasks = services.Where(_ => _.ImplementationType == startupTaskType).ToList();
                if (startupTasks.Count == 0)
                {
                    throw new InvalidOperationException($"The startup task '{StartupTaskTypeName}' is not registered; the harness no longer knows how to keep it out of the measurement");
                }

                foreach (var descriptor in startupTasks)
                {
                    services.Remove(descriptor);
                }
            });
        });

        return builder.Build();
    }

    /// <summary>
    /// Ensures the event store namespace and registers the measurement's client connection with the silo.
    /// </summary>
    /// <param name="host">The started host.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task ConnectClient(IHost host)
    {
        var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
        var siloAddress = host.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;
        var storage = host.Services.GetRequiredService<IStorage>();
        await storage.GetEventStore(EventStore).Namespaces.Ensure(Namespace);
        await grainFactory.GetConnectedClients(siloAddress).OnClientConnected(Connection, "footprint", false, Environment.ProcessId, "footprint", Environment.MachineName, "footprint");
    }

    /// <summary>
    /// Subscribes observers for the measurement's client connection.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="siloAddress">The silo the client is connected to.</param>
    /// <param name="prefix">The prefix of the observer identifiers.</param>
    /// <param name="count">The number of observers.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Activate(IGrainFactory grainFactory, SiloAddress siloAddress, string prefix, int count)
    {
        var eventTypes = new[] { new EventType("footprint-event", EventTypeGeneration.First) };
        for (var batch = 0; batch < count; batch += 100)
        {
            var tasks = Enumerable.Range(batch, Math.Min(100, count - batch)).Select(index =>
            {
                var client = new ConnectedClient
                {
                    ConnectionId = Connection,
                    Version = "footprint",
                    ClientType = "footprint",
                    LastSeen = DateTimeOffset.UtcNow
                };
                return Observer(grainFactory, prefix, index).Subscribe<IReactorObserverSubscriber>(
                    ObserverType.Reactor,
                    eventTypes,
                    siloAddress,
                    client);
            });
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Reads a whole-number command line argument.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="name">The name of the argument.</param>
    /// <param name="defaultValue">The value when the argument is not given.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">The argument is not a whole number of zero or more.</exception>
    internal static int IntArgument(string[] args, string name, int defaultValue)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0)
        {
            return defaultValue;
        }

        if (index + 1 >= args.Length || !int.TryParse(args[index + 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value) || value < 0)
        {
            throw new ArgumentException($"{name} needs a whole number of zero or more, got '{(index + 1 < args.Length ? args[index + 1] : string.Empty)}'", nameof(args));
        }

        return value;
    }

    static async Task Unsubscribe(IGrainFactory grainFactory, string prefix, int count)
    {
        for (var batch = 0; batch < count; batch += 100)
        {
            await Task.WhenAll(Enumerable.Range(batch, Math.Min(100, count - batch)).Select(index => Observer(grainFactory, prefix, index).Unsubscribe()));
        }
    }

    static async Task<int> CountSubscribed(IGrainFactory grainFactory, string prefix, int count)
    {
        var subscribed = 0;
        for (var batch = 0; batch < count; batch += 100)
        {
            var states = await Task.WhenAll(Enumerable.Range(batch, Math.Min(100, count - batch)).Select(index => Observer(grainFactory, prefix, index).IsSubscribed()));
            subscribed += states.Count(_ => _);
        }

        return subscribed;
    }

    static IObserver Observer(IGrainFactory grainFactory, string prefix, int index) =>
        grainFactory.GetGrain<IObserver>(new ObserverKey(new ObserverId($"{prefix}-{index}"), EventStore, Namespace, EventSequenceId.Log));

    static async Task<int> ActivationsOf<TGrain>(IGrainFactory grainFactory)
    {
        var statistics = await grainFactory.GetGrain<IManagementGrain>(0).GetSimpleGrainStatistics();
        var grainType = $"{typeof(TGrain).FullName},";
        return statistics
            .Where(_ => _.GrainType.StartsWith(grainType, StringComparison.Ordinal))
            .Sum(_ => _.ActivationCount);
    }

    static long LiveHeap()
    {
        for (var pass = 0; pass < 3; pass++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }

        return GC.GetTotalMemory(forceFullCollection: true);
    }

    static async Task Report(IGrainFactory grainFactory, string phase, long baseline, long measured, int baselineActivations, int count)
    {
        var delta = measured - baseline;
        var observers = await ActivationsOf<Observer>(grainFactory);
        var alerts = await ActivationsOf<Observation.Alerts.ObserverAlerts>(grainFactory);
        Console.WriteLine($"{phase}: live heap {measured / 1024.0 / 1024.0:F1} MiB, {delta / 1024.0 / 1024.0:+0.0;-0.0} MiB over baseline");
        Console.WriteLine($"  observer activations resident: {observers} (baseline {baselineActivations}), companion observer alert activations: {alerts}");
        Console.WriteLine($"  retained per measured observer: {delta / (double)count / 1024.0:F1} KiB");
    }

    static async Task Hold(int seconds)
    {
        if (seconds <= 0)
        {
            return;
        }

        // Time to take a heap snapshot, for example: dotnet-gcdump collect -p <pid>.
        Console.WriteLine($"  holding {seconds} s for a heap snapshot of process {Environment.ProcessId}");
        await Task.Delay(TimeSpan.FromSeconds(seconds));
    }
}
