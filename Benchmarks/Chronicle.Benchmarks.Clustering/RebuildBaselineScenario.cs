// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Cratis.Chronicle.Events;
using MongoDB.Bson;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Runs a bounded rebuild baseline with an independent twin reference.</summary>
internal static class RebuildBaselineScenario
{
    /// <summary>Runs one baseline and prints its JSON verdict.</summary>
    /// <param name="scenario">The scenario measurement input.</param>
    /// <returns>A task that completes after the run has reported its verdict.</returns>
    internal static async Task Run(string scenario)
    {
        var settings = RebuildBaselineSettings.Read(scenario);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        var counters = new BenchmarkCounters(nameof(RebuildBaselineModel));
        await using var fixture = new ClusterBenchmarkFixture(settings.Cluster, counters.Configure);
        try
        {
            await fixture.Start().WaitAsync(deadline.Token);
            var dataset = new RebuildBaselineDataset(fixture, settings);
            await dataset.Seed(deadline.Token);
            await Measure(fixture, dataset, counters, deadline.Token);
        }
        catch (Exception error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { Environment = fixture.TopologyDescription, Settings = settings, Verdict = "failed", Error = error.Message }));
            Environment.ExitCode = 1;
        }
    }

    static async Task Measure(ClusterBenchmarkFixture fixture, RebuildBaselineDataset dataset, BenchmarkCounters counters, CancellationToken deadline)
    {
        var startup = await dataset.Fence();
        var original = await dataset.Generation() ?? throw new InvalidOperationException(
            $"No served collection UUID before rebuild: '{dataset.ContainerName}' is not among [{string.Join(", ", await dataset.CollectionNames())}].");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(deadline);
        using var probeStop = CancellationTokenSource.CreateLinkedTokenSource(deadline);
        using var process = Process.GetCurrentProcess();
        var allocated = GC.GetTotalAllocatedBytes();
        var peak = process.WorkingSet64;
        var clock = Stopwatch.StartNew();
        var acknowledgements = new ConcurrentQueue<double>();
        var liveAcks = new ConcurrentQueue<double>();
        var list = new RebuildBaselineProbe(dataset.ReadListMarker, clock, acknowledgements);
        var key = new RebuildBaselineProbe(dataset.ReadKeyMarker, clock, acknowledgements);
        var workload = dataset.Settings.Mode == "live" ? Produce(dataset.AppendLive, dataset.Settings.LiveRate, (clock, liveAcks), stop.Token) : Task.CompletedTask;
        var markers = dataset.Settings.Mode == "live" ? Produce(() => dataset.AppendMarker(acknowledgements.Count + 1), 10, (clock, acknowledgements), stop.Token) : Task.CompletedTask;
        counters.Start(dataset.ContainerName);
        var requested = clock.Elapsed.TotalSeconds;
        var sampling = SampleContinuously(list, key, probeStop.Token);
        var replay = dataset.Replay();
        var measurement = new Measurement(fixture, dataset, counters, deadline)
        {
            Startup = startup,
            Stop = stop,
            ProbeStop = probeStop,
            Process = process,
            Allocated = allocated,
            Peak = peak,
            Clock = clock,
            LiveAcks = liveAcks,
            List = list,
            Key = key,
            Workload = workload,
            Markers = markers,
            Requested = requested,
            Sampling = sampling,
            Replay = replay
        };
        try
        {
            await measurement.AwaitSwap(original);
            await measurement.CompleteAndReport();
        }
        catch (Exception error)
        {
            await measurement.ReportFailure(error);
        }
        finally
        {
            counters.Stop();
            await stop.CancelAsync();
            await probeStop.CancelAsync();
        }
    }

    static async Task SampleContinuously(RebuildBaselineProbe list, RebuildBaselineProbe key, CancellationToken stop)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(25));
        try
        {
            do
            {
                await Task.WhenAll(list.Sample(cancellationToken: stop), key.Sample(cancellationToken: stop));
            }
            while (await timer.WaitForNextTickAsync(stop));
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            // The final sample remains censored rather than becoming a late observed latency.
        }
    }

    static async Task Produce(Func<Task> append, int rate, (Stopwatch Clock, ConcurrentQueue<double> Acknowledgements) timing, CancellationToken stop)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1d / rate));
        try
        {
            while (await timer.WaitForNextTickAsync(stop))
            {
                await append();
                timing.Acknowledgements.Enqueue(timing.Clock.Elapsed.TotalSeconds);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            // No append is cancelled; only the wait for the next tick observes shutdown.
        }
    }

    sealed class Measurement(ClusterBenchmarkFixture fixture, RebuildBaselineDataset dataset, BenchmarkCounters counters, CancellationToken deadline)
    {
        public required EventSequenceNumber Startup { get; init; }
        public required CancellationTokenSource Stop { get; init; }
        public required CancellationTokenSource ProbeStop { get; init; }
        public required Process Process { get; init; }
        public required long Allocated { get; init; }
        public required long Peak { get; set; }
        public required Stopwatch Clock { get; init; }
        public required ConcurrentQueue<double> LiveAcks { get; init; }
        public required RebuildBaselineProbe List { get; init; }
        public required RebuildBaselineProbe Key { get; init; }
        public required Task Workload { get; init; }
        public required Task Markers { get; init; }
        public required double Requested { get; init; }
        public required Task Sampling { get; init; }
        public required Task Replay { get; init; }
        double Swap { get; set; }

        public async Task AwaitSwap(BsonValue original)
        {
            while ((await dataset.Generation().WaitAsync(deadline))?.Equals(original) != false)
            {
                await ObserveFaults();
                RefreshPeak();
                await Task.Delay(25, deadline);
            }
            Swap = Clock.Elapsed.TotalSeconds;
        }

        public async Task CompleteAndReport()
        {
            await dataset.Cover(Startup, deadline);
            var coverage = Clock.Elapsed.TotalSeconds;
            var counts = counters.Stop();
            await Stop.CancelAsync();
            var fence = await DrainProducers();
            await dataset.Cover(fence, deadline);
            var covered = Clock.Elapsed.TotalSeconds;
            await CompleteProbesAndReplay();
            var activations = await fixture.GrainActivations(deadline);
            RefreshPeak();
            var rebuilt = await dataset.Snapshot(dataset.ContainerName, deadline);
            var twin = await dataset.Snapshot(dataset.TwinContainerName, deadline);
            var differences = Compare(rebuilt, twin);
            await ReportSuccess(new(coverage, covered, fence, activations, counts.MongoCommands, counts.GrainCalls, rebuilt.Count, differences));
            if (differences.Count != 0) Environment.ExitCode = 1;
        }

        public async Task ReportFailure(Exception error)
        {
            await Stop.CancelAsync();
            await ProbeStop.CancelAsync();
            try
            {
                await Task.WhenAll(Workload, Markers, Sampling, Replay).WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (Exception drainError)
            {
                error = new AggregateException(error, drainError);
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Commit = await RebuildBaselineEnvironment.Commit(),
                RebuildBaselineEnvironment.KernelVersion,
                RebuildBaselineEnvironment.OrleansVersion,
                Machine = RebuildBaselineEnvironment.Machine(),
                dataset.Settings,
                Storage = "mongodb",
                Environment = fixture.TopologyDescription,
                StartupFence = Startup.Value,
                RebuildStarted = Swap > Requested,
                ServedList = List.Report(deadline.IsCancellationRequested),
                KernelFindByKey = Key.Report(deadline.IsCancellationRequested),
                Verdict = "failed",
                Error = error.Message
            }));
            Environment.ExitCode = 1;
        }

        async Task ObserveFaults()
        {
            if (Replay.IsFaulted) await Replay;
            if (Workload.IsFaulted) await Workload;
            if (Markers.IsFaulted) await Markers;
            if (Sampling.IsFaulted) await Sampling;
        }

        void RefreshPeak()
        {
            Process.Refresh();
            Peak = Math.Max(Peak, Process.WorkingSet64);
        }

        async Task<EventSequenceNumber> DrainProducers()
        {
            // Cancellation stops scheduling, not appends: both producers await their current acknowledgement.
            await Task.WhenAll(Workload, Markers).WaitAsync(deadline);
            return await dataset.Fence().WaitAsync(deadline);
        }

        async Task CompleteProbesAndReplay()
        {
            await ProbeStop.CancelAsync();
            await Sampling.WaitAsync(deadline);
            await Task.WhenAll(List.Sample(recordObserved: false, cancellationToken: deadline), Key.Sample(recordObserved: false, cancellationToken: deadline));
            await Replay.WaitAsync(deadline);
        }

        List<string> Compare(IReadOnlyDictionary<string, string> rebuilt, IReadOnlyDictionary<string, string> twin)
        {
            var differences = RebuildBaselineComparison.Compare(rebuilt, twin).ToList();
            if (MissingLiveOverlap()) differences.Add("No acknowledged live append overlapped the rebuild window.");
            return differences;
        }

        bool MissingLiveOverlap() => dataset.Settings.Mode == "live" && !LiveAcks.ToArray().Any(at => at >= Requested && at <= Swap);

        async Task ReportSuccess(MeasurementResult result)
        {
            var settings = dataset.Settings;
            var throughput = RebuildBaselineMath.Throughput(dataset.SubscribedEvents, Requested, result.Coverage);
            var metrics = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["events-per-second"] = throughput,
                ["request-to-swap-seconds"] = Swap - Requested,
                ["post-swap-catch-up-seconds"] = result.Covered - Swap,
                ["mongo-commands-per-event"] = (double)result.MongoCommands / dataset.SubscribedEvents,
                ["grain-calls-per-event"] = (double)result.GrainCalls / dataset.SubscribedEvents
            };
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Commit = await RebuildBaselineEnvironment.Commit(),
                RebuildBaselineEnvironment.KernelVersion,
                RebuildBaselineEnvironment.OrleansVersion,
                Machine = RebuildBaselineEnvironment.Machine(),
                Settings = settings,
                Storage = "mongodb",
                Environment = fixture.TopologyDescription,
                GrainActivations = result.Activations,
                StartupFence = Startup.Value,
                F = result.Fence.Value,
                SubscribedEventsThroughStartupFence = dataset.SubscribedEvents,
                RebuildEventsPerSecond = throughput,
                RequestToServedSwapSeconds = Swap - Requested,
                StartupFenceCoverageSeconds = result.Coverage - Requested,
                PostSwapCatchUpSeconds = result.Covered - Swap,
                RebuildStarted = Swap > Requested,
                LiveAppends = LiveAcks.Count,
                LiveAppendsDuringRebuild = LiveAcks.Count(at => at >= Requested && at <= Swap),
                ServedList = List.Report(false),
                KernelFindByKey = Key.Report(false),
                PeakWorkingSetBytes = Peak,
                AllocatedBytes = GC.GetTotalAllocatedBytes() - Allocated,
                MemoryBoundary = "Process-wide, including all in-process silos, client, probes and twin projection",
                result.MongoCommands,
                result.GrainCalls,
                CounterBoundary = "Request through startup-fence coverage; kernel Mongo clients and all incoming grain dispatch, including twin, live load, probes and background work; ratios divide by subscribed seed events",
                Series = metrics.Select(metric => new { Name = $"Rebuild/{settings.Scenario}/mongodb/{settings.Topology}/{settings.Mode}/{metric.Key}", Label = "raw", metric.Value }),
                result.DocumentsCompared,
                result.Differences,
                Verdict = result.Differences.Count == 0 ? "passed" : "failed"
            }));
        }
    }

    sealed record MeasurementResult(double Coverage, double Covered, EventSequenceNumber Fence, IReadOnlyList<object> Activations, long MongoCommands, long GrainCalls, int DocumentsCompared, List<string> Differences);
}
