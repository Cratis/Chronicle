// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Runs one MongoDB rebuild with rate-limited live appends and independent served-list and kernel-key probes.
/// </summary>
internal static class LiveRebuildScenario
{
    /// <summary>
    /// Runs the bounded live-rebuild scenario and writes its measurements as JSON.
    /// </summary>
    /// <returns>A task representing the scenario execution.</returns>
    internal static async Task Run()
    {
        var eventCount = PositiveSetting("REBUILD_EVENTS", 10_000);
        var rate = PositiveSetting("REBUILD_LIVE_RATE", 10);
        var sampleMs = PositiveSetting("REBUILD_SAMPLE_MS", 100);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(PositiveSetting("REBUILD_TIMEOUT_SECONDS", 240)));
        using var producerStop = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        var fixture = new ClusterBenchmarkFixture(ClusterTopology.SingleSilo);
        await fixture.Start();
        try
        {
            var dataset = new LiveRebuildDataset(fixture, eventCount);
            await dataset.Seed().WaitAsync(deadline.Token);
            await Measure(dataset, eventCount, rate, sampleMs, producerStop, deadline.Token);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    static async Task Measure(LiveRebuildDataset dataset, int eventCount, int rate, int sampleMs, CancellationTokenSource producerStop, CancellationToken deadline)
    {
        var original = await dataset.ServedGeneration() ?? throw new InvalidOperationException("The served collection has no UUID.");
        using var process = Process.GetCurrentProcess();
        var allocatedBefore = GC.GetTotalAllocatedBytes();
        var clock = Stopwatch.StartNew();
        var phases = new Dictionary<string, double>(StringComparer.Ordinal);
        var acknowledgements = new ConcurrentQueue<double>();
        var listProbe = new RebuildReadProbe(dataset.ReadListMarker, clock, acknowledgements);
        var keyProbe = new RebuildReadProbe(dataset.ReadKeyMarker, clock, acknowledgements);
        var peakWorkingSet = process.WorkingSet64;
        var phase = "rebuild";
        var producer = Produce(dataset, rate, clock, acknowledgements, producerStop.Token);
        var replay = dataset.Replay();
        phases.Add("requested", clock.Elapsed.TotalSeconds);
        var swapSeconds = 0d;
        var rebuildSamples = 0;
        var liveAtSwap = 0;
        try
        {
            while (true)
            {
                deadline.ThrowIfCancellationRequested();
                if (replay.IsFaulted) await replay;
                if (producer.IsFaulted) await producer;
                var generation = await dataset.ServedGeneration().WaitAsync(deadline);
                if (phase == "rebuild" && generation?.Equals(original) == false)
                {
                    swapSeconds = clock.Elapsed.TotalSeconds;
                    phases.Add("served-generation-changed", swapSeconds);
                    phase = "post-swap-catch-up";
                    liveAtSwap = acknowledgements.Count;

                    // A frozen acknowledgement fence makes the catch-up measurement reproducible.
                    await producerStop.CancelAsync();
                    await producer;
                }
                await Task.WhenAll(listProbe.Sample(phase), keyProbe.Sample(phase)).WaitAsync(deadline);
                process.Refresh();
                peakWorkingSet = Math.Max(peakWorkingSet, process.WorkingSet64);
                if (phase == "rebuild") rebuildSamples++;
                if (phase != "rebuild" && listProbe.VisibleRevision >= acknowledgements.Count && keyProbe.VisibleRevision >= acknowledgements.Count) break;
                await Task.Delay(sampleMs, deadline);
            }
            phases.Add("frozen-live-fence-visible", clock.Elapsed.TotalSeconds);
            await replay.WaitAsync(deadline);
            if (rebuildSamples == 0 || liveAtSwap == 0)
            {
                throw new InvalidOperationException("No sampled rebuild/live overlap. Increase REBUILD_EVENTS or REBUILD_LIVE_RATE.");
            }
            await dataset.Handler.WaitTillActive().WaitAsync(deadline);
            if ((await dataset.Handler.GetFailedPartitions()).Any()) throw new InvalidOperationException("Replay has failed partitions.");
            await dataset.Verify().WaitAsync(deadline);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Scenario = "mongo-live-rebuild-join",
                HistoricalEvents = eventCount + 2,
                TargetLiveEventsPerSecond = rate,
                SampleIntervalMs = sampleMs,
                LiveAcknowledgementsAtSwap = liveAtSwap,
                LiveAcknowledgements = acknowledgements.Count,
                ObservedLiveEventsPerSecond = acknowledgements.Count / acknowledgements.Last(),
                RebuildThroughSwapSeconds = swapSeconds,
                PostSwapCatchUpSeconds = phases["frozen-live-fence-visible"] - swapSeconds,
                HistoricalEventsPerSecond = (eventCount + 2) / swapSeconds,
                RebuildSamples = rebuildSamples,
                SampledPeakProcessWorkingSetBytes = peakWorkingSet,
                ProcessAllocatedBytes = GC.GetTotalAllocatedBytes() - allocatedBefore,
                Phases = phases,
                ServedList = listProbe.Report(),
                KernelKeyLookup = keyProbe.Report()
            }));
        }
        finally
        {
            await producerStop.CancelAsync();
            await producer;
        }
    }

    static async Task Produce(LiveRebuildDataset dataset, int rate, Stopwatch clock, ConcurrentQueue<double> acknowledgements, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1d / rate));
        var revision = 0;
        try
        {
            // PeriodicTimer coalesces missed ticks: slow appends never trigger a catch-up burst.
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await dataset.AppendMarker(++revision);
                acknowledgements.Enqueue(clock.Elapsed.TotalSeconds);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal producer shutdown at swap or the bounded scenario deadline.
        }
    }

    static int PositiveSetting(string name, int fallback)
    {
        if (Environment.GetEnvironmentVariable(name) is not { } value)
        {
            return fallback;
        }

        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : throw new ArgumentException($"{name} must be a positive integer.");
    }
}
