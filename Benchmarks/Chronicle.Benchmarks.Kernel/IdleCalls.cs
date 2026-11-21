// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Measures the grain calls and the allocations of idle subscribed observers.
/// </summary>
/// <remarks>
/// An in-process silo with in-memory storage subscribes <c language="bash">--count</c> reactor observers to one client connection and
/// leaves them idle, with nothing appended, for <c language="bash">--seconds</c> seconds after a settle period. An incoming grain
/// call filter counts every grain call by interface and method. The allocation figure is every byte the process allocated
/// in the window, so it includes the harness and the silo's own housekeeping. Run it with <c language="bash">--idle-calls</c>;
/// see the README.
/// </remarks>
public static class IdleCalls
{
    static readonly ConcurrentDictionary<string, long> _calls = new();
    static long _counting;

    /// <summary>
    /// Runs the measurement.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Run(string[] args)
    {
        var count = ObserverFootprint.IntArgument(args, "--count", 1000);
        var seconds = ObserverFootprint.IntArgument(args, "--seconds", 30);
        var watchdog = Math.Max(1, ObserverFootprint.IntArgument(args, "--watchdog", 5));
        var settle = ObserverFootprint.IntArgument(args, "--settle", (watchdog * 2) + 2);

        using var host = ObserverFootprint.BuildHost(
            0,
            0,
            silo => silo.AddIncomingGrainCallFilter(async context =>
            {
                if (Interlocked.Read(ref _counting) == 1)
                {
                    var name = $"{context.InterfaceMethod?.DeclaringType?.Name ?? context.InterfaceName}.{context.MethodName}";
                    _calls.AddOrUpdate(name, 1, (_, calls) => calls + 1);
                }

                await context.Invoke();
            }),
            watchdog);
        await host.StartAsync();
        await ObserverFootprint.ConnectClient(host);
        var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
        var siloAddress = host.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress;

        await ObserverFootprint.Activate(grainFactory, siloAddress, "idle", count);
        Console.WriteLine($"Idle observers: {count} subscribed reactor observers, watchdog interval {watchdog} s, settled {settle} s, measured over {seconds} s");
        await Task.Delay(TimeSpan.FromSeconds(settle));

        GC.Collect();
        using var process = Process.GetCurrentProcess();
        var processorTime = process.TotalProcessorTime;
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var collectionsBefore = GC.CollectionCount(0);
        var stopwatch = Stopwatch.StartNew();
        Interlocked.Exchange(ref _counting, 1);
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        Interlocked.Exchange(ref _counting, 0);
        stopwatch.Stop();
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        var collections = GC.CollectionCount(0) - collectionsBefore;
        process.Refresh();
        processorTime = process.TotalProcessorTime - processorTime;

        var elapsed = stopwatch.Elapsed.TotalSeconds;
        var perObserverMinute = 60 / elapsed / count;
        Console.WriteLine("  grain calls per observer per minute, by interface method:");
        foreach (var call in _calls.OrderByDescending(call => call.Value))
        {
            Console.WriteLine($"    {call.Key}: {call.Value * perObserverMinute:F2}");
        }

        var total = _calls.Values.Sum();
        Console.WriteLine($"  all grain calls: {total} in {elapsed:F1} s, {total * perObserverMinute:F2} per observer per minute");
        Console.WriteLine($"  bytes allocated per observer per minute: {allocated * perObserverMinute:F0} ({allocated / 1024.0 / 1024.0 / elapsed:F2} MiB per second in total)");
        Console.WriteLine($"  gen0 collections per minute: {collections * 60 / elapsed:F1}");
        Console.WriteLine($"  process CPU while idle: {processorTime.TotalSeconds / elapsed:F3} cores");

        await host.StopAsync();
        return 0;
    }
}
