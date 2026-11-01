// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Runs one matched, bounded allocation diagnostic, not a benchmark matrix.
/// </summary>
internal static class CatalogRegressionProof
{
    static readonly Dictionary<string, long[]> _counters = new(StringComparer.Ordinal);
    static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    [ThreadStatic]
    static Stack<(string Name, long Start, long Children)>? _frames;
    static string _phase = "cold";

    /// <summary>
    /// Runs the bounded diagnostic and writes measured counters.
    /// </summary>
    /// <param name="output">The output JSON path.</param>
    /// <param name="attribution">Whether to enable temporary allocation scopes.</param>
    /// <returns>Zero when behavioral and retention checks pass.</returns>
    /// <exception cref="InvalidOperationException">Discovery does not contain the requested event catalog.</exception>
    internal static async Task<int> Run(string output, bool attribution)
    {
        if (attribution) AppDomain.CurrentDomain.SetData("cat5496.attribution", (Action<string, bool>)Record);
        var catalog = StartupCatalog.Create(1024);
        var defaults = new Defaults(catalog);
        var samples = new List<object>();
        var snapshots = new List<object>();
        var references = new List<WeakReference>();
        var before = GC.GetTotalAllocatedBytes(true);
        using var root = (EventStoreForTesting)defaults.EventStore;
        samples.Add(new { Phase = "cold", Allocated = GC.GetTotalAllocatedBytes(true) - before });
        if (root.EventTypes.All.Count != 1024) throw new InvalidOperationException("Cold discovery lost events.");
        var workloads = new (string Name, Func<Task> Run)[]
        {
            ("store", () => ScenarioPreparationProof.StoreRun(catalog)),
            ("readmodel.prepare", () => ScenarioPreparationProof.PrepareReadModel(defaults)),
            ("readmodel.materialize", () => ScenarioPreparationProof.Materialize(defaults)),
            ("reactor", () => ScenarioPreparationProof.Deliver(defaults)),
            ("event", async () => await ScenarioPreparationProof.PrepareEvents(defaults, root))
        };
        foreach (var workload in workloads)
        {
            _phase = workload.Name + ".warmup";
            for (var index = 0; index < 3; index++) await workload.Run();
            for (var index = 0; index < 3; index++)
            {
                _phase = workload.Name + ".sample" + index;
                before = GC.GetTotalAllocatedBytes(true);
                var watch = Stopwatch.StartNew();
                await workload.Run();
                samples.Add(new { Phase = _phase, Allocated = GC.GetTotalAllocatedBytes(true) - before, Milliseconds = watch.Elapsed.TotalMilliseconds });
            }
        }
        _phase = "retention.warmup";
        await Deliver(defaults, references);
        references.Clear();
        var retainedBefore = ScenarioMeasurement.RetainedBytes();
        snapshots.Add(Snapshot("warm", retainedBefore));
        _phase = "retention.first50";
        await Batch(defaults, references);
        var first = ScenarioMeasurement.RetainedBytes();
        snapshots.Add(Snapshot("after50", first));
        _phase = "retention.second50";
        await Batch(defaults, references);
        var second = ScenarioMeasurement.RetainedBytes();
        snapshots.Add(Snapshot("after100", second));
        var live = references.Count(reference => reference.IsAlive);
        var passed = live == 0;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.GetName().Name?.StartsWith("Cratis.Chronicle", StringComparison.Ordinal) == true)
            .Select(assembly => new { assembly.FullName, assembly.Location, Mvid = assembly.ManifestModule.ModuleVersionId }).ToArray();
        var report = new { Process = Environment.ProcessId, Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Attribution = attribution, Samples = samples, Counters = _counters, Snapshots = snapshots, Assemblies = assemblies,
            Retention = new { RetainedBytesAreInformational = true, First50 = first - retainedBefore, Total100 = second - retainedBefore, Second50 = second - first, LiveScenarios = live, Deliveries = 100, Passed = passed },
            CounterSemantics = "[calls,inclusiveBytes,exclusiveBytes]; exclusive subtracts synchronous instrumented children, never add inclusive nested rows; opt-in callback bookkeeping remains overhead" };
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, _jsonOptions));
        Console.WriteLine($"catalog-regression-proof: output={output} process={Environment.ProcessId} informational-retained100={second - retainedBefore} informational-second50={second - first} live={live} pass={passed}");
        return passed ? 0 : 1;
    }

    static void Record(string name, bool enter)
    {
        var frames = _frames ??= new();
        if (enter)
        {
            frames.Push((name, GC.GetAllocatedBytesForCurrentThread(), 0));
            return;
        }
        var end = GC.GetAllocatedBytesForCurrentThread();
        var frame = frames.Pop();
        if (frame.Name != name) throw new InvalidOperationException("Unbalanced diagnostic scopes.");
        var inclusive = end - frame.Start;
        if (frames.TryPop(out var parent)) frames.Push((parent.Name, parent.Start, parent.Children + inclusive));
        lock (_counters)
        {
            var key = _phase + "/" + name;
            if (!_counters.TryGetValue(key, out var row)) _counters[key] = row = new long[3];
            row[0]++;
            row[1] += inclusive;
            row[2] += inclusive - frame.Children;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task Batch(Defaults defaults, List<WeakReference> references)
    {
        for (var index = 0; index < 50; index++) await Deliver(defaults, references);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task Deliver(Defaults defaults, List<WeakReference> references)
    {
        var probe = new CatalogDeliveryProbe();
        using var scenario = new ReactorScenario<CatalogReactor>(defaults);
        scenario.Services.AddSingleton(probe);
        await scenario.Given.ForEventSource("retention-source").Events(new CatalogRecorded());
        if (probe.Deliveries != 1) throw new InvalidOperationException("No actual reactor delivery.");
        references.Add(new(scenario));
        references.Add(new(probe));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object Snapshot(string stage, long retained)
    {
        var rows = new Dictionary<string, long>(StringComparer.Ordinal);
        var assembly = typeof(EventStoreForTesting).Assembly;
        foreach (var name in new[] { "TestingArtifactCatalogs", "SchemaGeneratorsForTesting", "ApplicationMetadataForTesting" })
        {
            var type = assembly.GetType("Cratis.Chronicle.Testing.Events." + name);
            if (type is null) continue;
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            {
                var value = field.GetValue(null);
                if (value is null) continue;
                rows[name + "." + field.Name] = Count(value);
                if (value.GetType().GetProperty("Values")?.GetValue(value) is not IEnumerable values) continue;
                foreach (var entry in values)
                {
                    var target = entry;
                    if (entry.GetType().IsGenericType && entry.GetType().GetGenericTypeDefinition() == typeof(Lazy<>)) target = entry.GetType().GetProperty("Value")!.GetValue(entry)!;
                    foreach (var content in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        var contents = content.GetValue(target);
                        if (contents is null) continue;
                        var key = name + ".contents." + content.Name;
                        rows[key] = rows.GetValueOrDefault(key) + Count(contents);
                        if (content.Name == "_schemas" && contents.GetType().GetProperty("Values")?.GetValue(contents) is IEnumerable templates)
                            foreach (var template in templates) rows["serialized.characters"] = rows.GetValueOrDefault("serialized.characters") + ((string)template).Length;
                        if (content.Name == "_seed")
                        {
                            var definitions = contents.GetType().GetField("_definitions", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(contents);
                            if (definitions is not null) rows["seed.definitions"] = rows.GetValueOrDefault("seed.definitions") + Count(definitions);
                        }
                    }
                }
            }
        }
        return new { Stage = stage, RetainedBytes = retained, Caches = rows };
    }

    static long Count(object value) => value.GetType().GetProperty("Count")?.GetValue(value) is int count ? count : 0;
}
