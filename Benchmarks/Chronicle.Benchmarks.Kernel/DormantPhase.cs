// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// The dormant phase of <c language="bash">--observer-footprint --dormant</c>: what subscribed observers cost while they have nothing
/// to do, once the kernel releases them from memory, and what waking them costs (decision 0034).
/// </summary>
/// <remarks>
/// A kernel that does not release dormant observers reports the same figures for the subscribed, released and woken
/// phases, which is what the baseline recorded before the release existed. The phase never asserts anything; it prints
/// the resident activations by grain type, the live heap, the grain calls and bytes allocated per observer per minute
/// over a window with nothing appended, and the time to wake the observers with a call.
/// </remarks>
public sealed class DormantPhase
{
    readonly ConcurrentDictionary<string, long> _calls = new();
    long _counting;

    /// <summary>
    /// Gets the incoming grain call filter that counts calls while a window is being measured.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    public void CountCalls(ISiloBuilder silo) =>
        silo.AddIncomingGrainCallFilter(async context =>
        {
            if (Interlocked.Read(ref _counting) == 1)
            {
                var name = $"{context.InterfaceMethod?.DeclaringType?.Name ?? context.InterfaceName}.{context.MethodName}";
                _calls.AddOrUpdate(name, 1, (_, calls) => calls + 1);
            }

            await context.Invoke();
        });

    /// <summary>
    /// Waits for the kernel to release dormant observers, then measures a window of nothing happening, then wakes them.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="prefix">The prefix of the observer identifiers.</param>
    /// <param name="count">The number of observers.</param>
    /// <param name="baseline">The live heap before the observers were activated.</param>
    /// <param name="settleSeconds">Seconds to wait for the release.</param>
    /// <param name="windowSeconds">Seconds to measure with nothing appended.</param>
    /// <param name="wakeSamples">How many single observers to wake one at a time, for latency percentiles.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Run(IGrainFactory grainFactory, string prefix, int count, long baseline, int settleSeconds, int windowSeconds, int wakeSamples)
    {
        Console.WriteLine($"dormant phase: waiting {settleSeconds} s for dormant observers to be released, then measuring {windowSeconds} s with nothing appended");
        await Task.Delay(TimeSpan.FromSeconds(settleSeconds));

        await ObserverFootprint.ReportResident(grainFactory, "released", baseline, count);
        await Window(count, windowSeconds);

        // Grain calls are counted over both wake steps, so the cost of waking is reported per woken observer.
        _calls.Clear();
        Interlocked.Exchange(ref _counting, 1);

        // One observer at a time first: the latency a dormant observer adds to the first event after it was released.
        var latencies = new List<double>();
        for (var index = 0; index < Math.Min(wakeSamples, count); index++)
        {
            var started = Stopwatch.GetTimestamp();
            await ObserverFootprint.ObserverFor(grainFactory, prefix, index).IsSubscribed();
            latencies.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        if (latencies.Count > 0)
        {
            latencies.Sort();
            Console.WriteLine($"  wake, one observer at a time: median {latencies[latencies.Count / 2]:F2} ms, p95 {latencies[(int)(latencies.Count * 0.95)]:F2} ms, max {latencies[^1]:F2} ms over {latencies.Count} observers");
        }

        // Then the whole population at once: the burst a single event can cause when many observers watch for it.
        var stopwatch = Stopwatch.StartNew();
        var subscribed = await ObserverFootprint.CountSubscribed(grainFactory, prefix, count);
        stopwatch.Stop();
        Interlocked.Exchange(ref _counting, 0);
        ReportWakeCalls(Math.Min(wakeSamples, count) + count);
        Console.WriteLine($"  wake, all {count} observers in batches of 100: {stopwatch.Elapsed.TotalMilliseconds:F0} ms in total, {stopwatch.Elapsed.TotalMilliseconds / count:F2} ms per observer; {subscribed} of {count} still subscribed");
        await ObserverFootprint.ReportResident(grainFactory, "woken", baseline, count);
        await Window(count, windowSeconds);
    }

    /// <summary>
    /// Activates the observers that were released again, and reports the grain calls and companion activations that cost.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="prefix">The prefix of the observer identifiers.</param>
    /// <param name="count">The number of observers.</param>
    /// <param name="residentBefore">The observer activations resident before the wake.</param>
    /// <returns>Awaitable task.</returns>
    public async Task WakeReleased(IGrainFactory grainFactory, string prefix, int count, int residentBefore)
    {
        _calls.Clear();
        Interlocked.Exchange(ref _counting, 1);
        await ObserverFootprint.CountSubscribed(grainFactory, prefix, count);
        Interlocked.Exchange(ref _counting, 0);
        var statistics = await grainFactory.GetGrain<IManagementGrain>(0).GetSimpleGrainStatistics();
        var residentAfter = statistics.Where(_ => _.GrainType.StartsWith($"{typeof(Observer).FullName},", StringComparison.Ordinal)).Sum(_ => _.ActivationCount);
        var activated = Math.Max(1, residentAfter - residentBefore);
        Console.WriteLine($"  wake of released observers: {residentAfter - residentBefore} observers were activated by the wake");
        ReportWakeCalls(activated);
        await ObserverFootprint.ReportResident(grainFactory, "after the wake of released observers", 0, count);
    }

    void ReportWakeCalls(int wakes)
    {
        var total = _calls.Values.Sum();
        var alerts = _calls.Where(call => call.Key.StartsWith("IObserverAlerts.", StringComparison.Ordinal)).Sum(call => call.Value);
        Console.WriteLine($"  wake calls: {total} grain calls over {wakes} wakes, {total / (double)wakes:F2} per wake; {alerts} to ObserverAlerts, {alerts / (double)wakes:F3} per wake");
        foreach (var call in _calls.OrderByDescending(call => call.Value).Take(8))
        {
            Console.WriteLine($"    {call.Key}: {call.Value / (double)wakes:F2} per wake");
        }
    }

    async Task Window(int count, int seconds)
    {
        _calls.Clear();
        GC.Collect();
        using var process = Process.GetCurrentProcess();
        var processorTime = process.TotalProcessorTime;
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        Interlocked.Exchange(ref _counting, 1);
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        Interlocked.Exchange(ref _counting, 0);
        stopwatch.Stop();
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        process.Refresh();
        processorTime = process.TotalProcessorTime - processorTime;

        var elapsed = stopwatch.Elapsed.TotalSeconds;
        var perObserverMinute = 60 / elapsed / count;
        var total = _calls.Values.Sum();
        Console.WriteLine($"  window of {elapsed:F1} s: {total} grain calls, {total * perObserverMinute:F2} per observer per minute; {allocated * perObserverMinute:F0} bytes allocated per observer per minute; process CPU {processorTime.TotalSeconds / elapsed:F3} cores");
        foreach (var call in _calls.OrderByDescending(call => call.Value).Take(8))
        {
            Console.WriteLine($"    {call.Key}: {call.Value * perObserverMinute:F2} per observer per minute");
        }
    }
}
