// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures the first open of a read model table after a restart on SQLite, as the number of stores and namespaces grows.
/// </summary>
/// <remarks>
/// Cold SQLite numbers show how cost scales on SQLite only. PostgreSQL and SQL Server timings, warm and cold, are
/// unverified. Scales come from <c>CHRONICLE_BENCH_SCALES</c> (comma separated <c>StoresxNamespaces</c>, default
/// <c>1x1,10x10,50x10</c>); the version label printed with the counts comes from <c>CHRONICLE_BENCH_VERSION</c>.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, iterationCount: 5, invocationCount: 1)]
public class ReadModelTableColdResolutionBenchmark
{
    ReadModelTableBenchmarkFixture? _fixture;

    /// <summary>
    /// Gets or sets the scale as <c>StoresxNamespaces</c>.
    /// </summary>
    [ParamsSource(nameof(Scales))]
    public string Scale { get; set; } = "1x1";

    /// <summary>
    /// Gets or sets the mode: <c>Unbound</c> has no table-name ledger rows, <c>Bound</c> has them from an earlier run.
    /// </summary>
    [Params("Unbound", "Bound")]
    public string Mode { get; set; } = "Unbound";

    /// <summary>
    /// Gets the scales to run.
    /// </summary>
    public static IEnumerable<string> Scales =>
        (Environment.GetEnvironmentVariable("CHRONICLE_BENCH_SCALES") ?? "1x1,10x10,50x10")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Seeds the fixture and prints the command, connection and context counts of one cold first open.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var parts = Scale.Split('x');
        _fixture = ReadModelTableBenchmarkFixture.Create("orders", int.Parse(parts[0]), int.Parse(parts[1])).GetAwaiter().GetResult();
        Prepare();
        using var counter = new EfCoreEventCounter();
        _fixture.Find().GetAwaiter().GetResult();
        var version = Environment.GetEnvironmentVariable("CHRONICLE_BENCH_VERSION") ?? "patch";
        Console.WriteLine($"PERF-COUNTS version={version} scale={Scale} mode={Mode} commands={counter.Commands} connections={counter.Connections} contexts={counter.Contexts}");
    }

    /// <summary>
    /// Builds a fresh instance and, when unbound, removes the ledger rows.
    /// </summary>
    [IterationSetup]
    public void Prepare()
    {
        _fixture!.NewInstance();
        if (Mode == "Unbound")
        {
            _fixture.DeleteLedger();
        }
    }

    /// <summary>
    /// Disposes the fixture.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => _fixture?.Dispose();

    /// <summary>
    /// Performs the first open on the fresh instance: resolution, provisioning and a query.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [Benchmark]
    public Task FirstOpen() => _fixture!.Find();
}
