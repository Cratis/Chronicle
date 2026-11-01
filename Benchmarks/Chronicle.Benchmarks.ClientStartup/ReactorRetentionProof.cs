// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Measures repeated real reactor delivery separately from store/provider reachability tests.
/// </summary>
internal static class ReactorRetentionProof
{
    const int BatchSize = 50;

    /// <summary>
    /// Delivers 100 fresh, disposed scenarios after warmup and checks that none stays alive.
    /// Retained bytes are reported for information only; they swing by several MB between
    /// in-process snapshots, so only the live-scenario count decides the result.
    /// </summary>
    /// <param name="defaults">The application-sized artifact catalog.</param>
    /// <returns>The measurements and whether every completed scenario was collected.</returns>
    internal static async Task<(string Summary, bool Passed)> Measure(Defaults defaults)
    {
        await Deliver(defaults);
        var references = new List<WeakReference>(BatchSize * 2);
        var before = ScenarioMeasurement.RetainedBytes();
        await RunBatch(defaults, references);
        var first = ScenarioMeasurement.RetainedBytes();
        await RunBatch(defaults, references);
        var second = ScenarioMeasurement.RetainedBytes();
        var survivors = references.Count(reference => reference.IsAlive);
        var growth = second - before;
        return ($"reactor-retention: deliveries=100 informational-retained-first50={first - before}B informational-retained-total100={growth}B informational-second50={second - first}B live-scenarios={survivors} pass={survivors == 0}", survivors == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task RunBatch(Defaults defaults, List<WeakReference> references)
    {
        for (var index = 0; index < BatchSize; index++) references.Add(await Deliver(defaults));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<WeakReference> Deliver(Defaults defaults)
    {
        var probe = new CatalogDeliveryProbe();
        using var scenario = new ReactorScenario<CatalogReactor>(defaults);
        scenario.Services.AddSingleton(probe);
        await scenario.Given.ForEventSource("retention-source").Events(new CatalogRecorded());
        if (probe.Deliveries != 1) throw new InvalidOperationException("Retention sample did not deliver exactly one event.");
        return new(scenario);
    }
}
