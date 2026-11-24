// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// One run of the multi-namespace GC workload in this process, for <see cref="GcExperiment"/> to start once per GC configuration.
/// </summary>
/// <remarks>
/// <para>
/// The run starts the in-process silo with in-memory storage, spreads reactor observers over a number of namespaces of
/// one event store and then, for a window, subscribes and unsubscribes them again and again. That is what dormancy does
/// to a node: activations come and go, and every cycle leaves short-lived allocations and, for what lives long enough,
/// gen 2 garbage. After the window nothing is forced: the process waits, so the committed memory it reports is what the
/// collector chose to keep, which is the figure that was far above the live heap on the production node. Only then does it
/// force full compacting collections to measure what was live.
/// </para>
/// <para>
/// It prints one line starting with <see cref="ResultPrefix"/> followed by the <see cref="GcRunResult"/> as JSON.
/// </para>
/// </remarks>
public static class GcWorkload
{
    /// <summary>
    /// The prefix of the line the run prints its result on.
    /// </summary>
    public const string ResultPrefix = "GCRESULT ";

    /// <summary>
    /// Runs the workload.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> Run(string[] args)
    {
        var namespaces = Math.Max(1, ObserverFootprint.IntArgument(args, "--namespaces", 20));
        var count = Math.Max(namespaces, ObserverFootprint.IntArgument(args, "--count", 2000));
        var window = ObserverFootprint.IntArgument(args, "--window", 30);
        var settle = ObserverFootprint.IntArgument(args, "--settle", 20);

        using var host = ObserverFootprint.BuildHost(idleSeconds: 0, collectionAgeSeconds: 5, watchdogSeconds: 1);
        await host.StartAsync();
        var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
        var siloAddress = host.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;
        await ObserverFootprint.ConnectClient(host);
        var storage = host.Services.GetRequiredService<IStorage>();
        for (var index = 0; index < namespaces; index++)
        {
            await storage.GetEventStore(ObserverFootprint.EventStore).Namespaces.Ensure(NamespaceName(index));
        }

        // One cycle first, so JIT and type caches are not part of the window.
        await Cycle(grainFactory, siloAddress, namespaces, Math.Min(count, 200));

        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var pauseBefore = GC.GetTotalPauseDuration();
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        var operations = 0L;
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(window))
        {
            operations += await Cycle(grainFactory, siloAddress, namespaces, count);
        }

        stopwatch.Stop();
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        var pause = GC.GetTotalPauseDuration() - pauseBefore;
        var jsonAfterLoad = TrackedJsonOptions();
        var collections = (GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2);

        await Task.Delay(TimeSpan.FromSeconds(settle));
        var settled = GC.GetGCMemoryInfo();
        var resident = Process.GetCurrentProcess().WorkingSet64;
        var heap = settled.HeapSizeBytes;
        var committed = settled.TotalCommittedBytes;
        var fragmentation = settled.GenerationInfo.Length > 2 ? settled.GenerationInfo[2].FragmentationAfterBytes : 0;

        for (var pass = 0; pass < 3; pass++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }

        var live = GC.GetTotalMemory(forceFullCollection: false);
        var jsonLive = TrackedJsonOptions();
        var committedAfterForce = GC.GetGCMemoryInfo().TotalCommittedBytes;

        var result = new GcRunResult(
            GCSettings.IsServerGC,
            operations,
            stopwatch.Elapsed.TotalSeconds,
            allocated,
            pause.TotalMilliseconds,
            collections.Item1,
            collections.Item2,
            collections.Item3,
            jsonAfterLoad,
            jsonLive,
            committed,
            resident,
            heap,
            fragmentation,
            live,
            committedAfterForce);
        Console.WriteLine(ResultPrefix + JsonSerializer.Serialize(result));

        await host.StopAsync();
        return 0;
    }

    /// <summary>
    /// Counts the System.Text.Json options instances the runtime tracks, which is how many are alive or not yet collected.
    /// </summary>
    /// <returns>The count, or -1 when the runtime does not expose the tracking table.</returns>
    static int TrackedJsonOptions()
    {
        var tracking = typeof(System.Text.Json.JsonSerializerOptions).GetNestedType("TrackedOptionsInstances", BindingFlags.NonPublic | BindingFlags.Static);
        var all = tracking?.GetProperty("All", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
            ?? tracking?.GetField("All", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        if (all is System.Collections.IEnumerable table)
        {
            return table.Cast<object>().Count();
        }

        return -1;
    }

    static string NamespaceName(int index) => $"tenant-{index}";

    static async Task<long> Cycle(IGrainFactory grainFactory, SiloAddress siloAddress, int namespaces, int count)
    {
        var eventTypes = new[] { new EventType("footprint-event", EventTypeGeneration.First) };
        for (var batch = 0; batch < count; batch += 100)
        {
            var size = Math.Min(100, count - batch);
            await Task.WhenAll(Enumerable.Range(batch, size).Select(index =>
            {
                var client = new ConnectedClient
                {
                    ConnectionId = ObserverFootprint.Connection,
                    Version = "footprint",
                    ClientType = "footprint",
                    LastSeen = DateTimeOffset.UtcNow
                };
                return ObserverFor(grainFactory, namespaces, index).Subscribe<IReactorObserverSubscriber>(ObserverType.Reactor, eventTypes, siloAddress, client);
            }));
        }

        for (var batch = 0; batch < count; batch += 100)
        {
            await Task.WhenAll(Enumerable.Range(batch, Math.Min(100, count - batch)).Select(index => ObserverFor(grainFactory, namespaces, index).Unsubscribe()));
        }

        return 2L * count;
    }

    static IObserver ObserverFor(IGrainFactory grainFactory, int namespaces, int index) =>
        grainFactory.GetGrain<IObserver>(new ObserverKey(new ObserverId($"gc-{index}"), ObserverFootprint.EventStore, NamespaceName(index % namespaces), EventSequenceId.Log));
}
