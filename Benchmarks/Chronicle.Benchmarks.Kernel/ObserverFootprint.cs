// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
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
/// </remarks>
public static class ObserverFootprint
{
    const string EventStore = "footprint";
    const string Namespace = "Default";

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

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{ChronicleOptions.SectionPath}:Storage:Type"] = StorageType.InMemory,
            [$"{ChronicleOptions.SectionPath}:Observers:WatchdogInterval"] = "1",
            [$"{ChronicleOptions.SectionPath}:Observers:IdleDeactivationAfter"] = idleSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
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

            silo.Services.AddTypeDiscovery();
            silo.Services.AddBindingsByConvention();
            silo.Services.AddSelfBindings();
            silo.AddChronicleToSilo(chronicle => chronicle.WithInMemory());
            silo.ConfigureServices(services =>
            {
                services.AddOptions<ChronicleOptions>().BindConfiguration(ChronicleOptions.SectionPath);

                // The startup task provisions system event stores and registers kernel artifacts; none of it is part
                // of what an observer activation retains, and it would add its own resident grains to the measurement.
                var startupTaskType = typeof(ChronicleOptions).Assembly.GetType("Orleans.Hosting.ChronicleServerStartupTask");
                foreach (var descriptor in services.Where(_ => _.ImplementationType == startupTaskType).ToList())
                {
                    services.Remove(descriptor);
                }
            });
        });

        using var host = builder.Build();
        await host.StartAsync();
        var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
        var siloAddress = host.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;
        var storage = host.Services.GetRequiredService<IStorage>();
        await storage.GetEventStore(EventStore).Namespaces.Ensure(Namespace);

        Console.WriteLine($"Observer activation footprint: {count} observers, idle deactivation after {idleSeconds} s (0 = disabled), Orleans collection age {(collectionAgeSeconds > 0 ? $"{collectionAgeSeconds} s" : "default")}");

        // Warm up JIT, type caches and shared singletons so they are not attributed to the measured activations.
        await Activate(grainFactory, siloAddress, "warmup", warmup);
        await Unsubscribe(grainFactory, "warmup", warmup);

        var baseline = LiveHeap();
        var baselineActivations = await ActivationsOf<Observer>(grainFactory);

        var stopwatch = Stopwatch.StartNew();
        await Activate(grainFactory, siloAddress, "measured", count);
        stopwatch.Stop();
        var subscribed = LiveHeap();
        await Report(grainFactory, "subscribed", baseline, subscribed, baselineActivations, count);
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
        await Hold(IntArgument(args, "--hold-unsubscribed", 0));

        await host.StopAsync();
        return 0;
    }

    static async Task Activate(IGrainFactory grainFactory, SiloAddress siloAddress, string prefix, int count)
    {
        var eventTypes = new[] { new EventType("footprint-event", EventTypeGeneration.First) };
        for (var batch = 0; batch < count; batch += 100)
        {
            var tasks = Enumerable.Range(batch, Math.Min(100, count - batch)).Select(index =>
            {
                var client = new ConnectedClient
                {
                    ConnectionId = new ConnectionId($"{prefix}-connection-{index}"),
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

    static async Task Unsubscribe(IGrainFactory grainFactory, string prefix, int count)
    {
        for (var batch = 0; batch < count; batch += 100)
        {
            await Task.WhenAll(Enumerable.Range(batch, Math.Min(100, count - batch)).Select(index => Observer(grainFactory, prefix, index).Unsubscribe()));
        }
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

    static int IntArgument(string[] args, string name, int defaultValue)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var value) ? value : defaultValue;
    }
}
