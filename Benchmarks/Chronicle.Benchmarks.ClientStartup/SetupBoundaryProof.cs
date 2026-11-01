// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Measures synchronous construction and disposal boundaries once cold and once warm.
/// </summary>
internal static class SetupBoundaryProof
{
    static readonly Dictionary<string, long[]> _counters = new(StringComparer.Ordinal);
    static readonly Stack<(string Name, long Start, long Children)> _frames = new();
    static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    static string _phase = "cold";

    /// <summary>
    /// Runs one bounded lifecycle diagnostic with the fixed 1024-event catalog.
    /// </summary>
    /// <param name="output">The measured output path.</param>
    /// <returns>Zero after both lifecycles complete with the full event catalog.</returns>
    internal static int Run(string output)
    {
        AppDomain.CurrentDomain.SetData("cat5496.setup", (Action<string, bool>)Record);
        var catalog = StartupCatalog.Create(1024);
        var samples = new List<object>();
        Measure("cold", () => new EventStoreForTesting(null, catalog), samples);
        Measure("warm", () => new EventStoreForTesting(null, catalog), samples);
        var report = new
        {
            Process = Environment.ProcessId,
            Thread = Environment.CurrentManagedThreadId,
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Samples = samples,
            Counters = _counters,
            CounterSemantics = "[calls,inclusive current-thread bytes,exclusive current-thread bytes]; synchronous spans only; do not sum inclusive parent and child rows"
        };
        File.WriteAllText(output, JsonSerializer.Serialize(report, _jsonOptions));
        Console.WriteLine($"setup-boundary-proof: process={Environment.ProcessId} output={output} cold=1 warm=1");
        return 0;
    }

    static void Measure(string phase, Func<EventStoreForTesting> create, List<object> samples)
    {
        _phase = phase;
        var startUtc = DateTime.UtcNow;
        var allBefore = GC.GetTotalAllocatedBytes(true);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var store = create();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var allAllocated = GC.GetTotalAllocatedBytes(true) - allBefore;
        var endUtc = DateTime.UtcNow;
        samples.Add(new { Phase = phase + ".constructor", StartUtc = startUtc, EndUtc = endUtc, CurrentThreadBytes = allocated, AllThreadsBytes = allAllocated });
        if (store.EventTypes.All.Count != 1024) throw new InvalidOperationException("Discovery lost catalog events.");
        startUtc = DateTime.UtcNow;
        allBefore = GC.GetTotalAllocatedBytes(true);
        before = GC.GetAllocatedBytesForCurrentThread();
        store.Dispose();
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allAllocated = GC.GetTotalAllocatedBytes(true) - allBefore;
        endUtc = DateTime.UtcNow;
        samples.Add(new { Phase = phase + ".dispose", StartUtc = startUtc, EndUtc = endUtc, CurrentThreadBytes = allocated, AllThreadsBytes = allAllocated });
    }

    static void Record(string name, bool enter)
    {
        if (enter)
        {
            _frames.Push((name, GC.GetAllocatedBytesForCurrentThread(), 0));
            return;
        }
        var end = GC.GetAllocatedBytesForCurrentThread();
        var frame = _frames.Pop();
        if (frame.Name != name) throw new InvalidOperationException("Unbalanced setup allocation scopes.");
        var inclusive = end - frame.Start;
        if (_frames.TryPop(out var parent)) _frames.Push((parent.Name, parent.Start, parent.Children + inclusive));
        var key = _phase + "/" + name;
        if (!_counters.TryGetValue(key, out var row)) _counters[key] = row = new long[3];
        row[0]++;
        row[1] += inclusive;
        row[2] += inclusive - frame.Children;
    }
}
