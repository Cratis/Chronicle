// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Chronicle.Testing.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Runs a bounded, fresh-process proof over an application-sized catalog using baseline-compatible public APIs.
/// </summary>
internal static class ScenarioPreparationProof
{
    const int EventCount = 1024;

    /// <summary>
    /// Measures preparation, verified materialization/delivery, and repeated reactor release.
    /// </summary>
    /// <param name="output">The path receiving the measurements.</param>
    /// <returns>Zero when no completed scenario survives collection and all behavioral checks pass.</returns>
    /// <exception cref="InvalidOperationException">The application catalog was not discovered.</exception>
    internal static async Task<int> Run(string output)
    {
        var catalog = StartupCatalog.Create(EventCount);
        var defaults = new Defaults(catalog);
        var allocated = GC.GetTotalAllocatedBytes(true);
        var watch = Stopwatch.StartNew();
        using var root = (EventStoreForTesting)defaults.EventStore;
        if (root.EventTypes.All.Count != EventCount) throw new InvalidOperationException("The application catalog was not discovered.");
        var lines = new List<string>
        {
            $"# {DateTimeOffset.UtcNow:O} events={EventCount} reducers={catalog.Reducers.Count()} samples=30 process={Environment.ProcessId}",
            $"cold-root: {watch.Elapsed.TotalMilliseconds:F2}ms/{GC.GetTotalAllocatedBytes(true) - allocated}B (catalog emission excluded)",
            await ScenarioMeasurement.Measure("store-discovery-and-seed", () => StoreRun(catalog)),
            await ScenarioMeasurement.Measure("read-model-prepare", () => PrepareReadModel(defaults)),
            await ScenarioMeasurement.Measure("read-model-materialize", () => Materialize(defaults)),
            await ScenarioMeasurement.Measure("reactor-deliver-and-dispose", () => Deliver(defaults)),
            await ScenarioMeasurement.Measure("event-scenario-prepare", () => PrepareEvents(defaults, root))
        };
        var retention = await ReactorRetentionProof.Measure(defaults);
        lines.Add(retention.Summary);
        using var process = Process.GetCurrentProcess();
        lines.Add($"peak-working-set={process.PeakWorkingSet64}B");
        await File.WriteAllLinesAsync(output, lines);
        foreach (var line in lines) Console.WriteLine(line);
        return retention.Passed ? 0 : 1;
    }

    /// <summary>
    /// Creates and disposes one equivalent test store.
    /// </summary>
    /// <param name="catalog">The artifact catalog.</param>
    /// <returns>The completed store run.</returns>
    /// <exception cref="InvalidOperationException">The store lost event registrations.</exception>
    internal static Task StoreRun(InMemoryClientArtifactsProvider catalog)
    {
        using var store = new EventStoreForTesting(null, catalog);
        if (store.EventTypes.All.Count != EventCount) throw new InvalidOperationException("Fresh store lost event registrations.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Prepares one read-model scenario without materialization.
    /// </summary>
    /// <param name="defaults">The run defaults.</param>
    /// <returns>The completed preparation.</returns>
    internal static Task PrepareReadModel(Defaults defaults)
    {
        using var scenario = new ReadModelScenario<CatalogModel<CatalogRecorded>>(null, defaults);
        _ = scenario.ReadModels;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Materializes and checks one real read model.
    /// </summary>
    /// <param name="defaults">The run defaults.</param>
    /// <returns>The asynchronous workload.</returns>
    /// <exception cref="InvalidOperationException">The read model is incorrect.</exception>
    internal static async Task Materialize(Defaults defaults)
    {
        using var scenario = new ReadModelScenario<CatalogModel<CatalogRecorded>>(null, defaults);
        await scenario.Given.ForEventSource("catalog-source").Events(new CatalogRecorded());
        if (scenario.Instance?.Count != 1) throw new InvalidOperationException("Materialization did not fold exactly one event.");
    }

    /// <summary>
    /// Delivers and checks one event through a real reactor.
    /// </summary>
    /// <param name="defaults">The run defaults.</param>
    /// <returns>The asynchronous workload.</returns>
    /// <exception cref="InvalidOperationException">The reactor did not handle the event.</exception>
    internal static async Task Deliver(Defaults defaults)
    {
        var probe = new CatalogDeliveryProbe();
        using var scenario = new ReactorScenario<CatalogReactor>(defaults);
        scenario.Services.AddSingleton(probe);
        await scenario.Given.ForEventSource("catalog-source").Events(new CatalogRecorded());
        if (probe.Deliveries != 1) throw new InvalidOperationException("Reactor did not receive exactly one event.");
    }

    /// <summary>
    /// Prepares one event scenario using the root store.
    /// </summary>
    /// <param name="defaults">The run defaults.</param>
    /// <param name="root">The root test store.</param>
    /// <returns>The completed preparation.</returns>
    internal static Task PrepareEvents(Defaults defaults, EventStoreForTesting root)
    {
        using var scenario = new EventScenario(EventSequenceId.Log, root.Name, root.Namespace, null, defaults);
        return Task.CompletedTask;
    }
}
