// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures the warm cost of opening a read model table scope through the SQL database and the SQL sink on SQLite.
/// </summary>
/// <remarks>
/// Warm SQLite results establish neither PostgreSQL nor SQL Server warm timings, and they do not establish first-open
/// cost on any provider. Allocated bytes per operation are the primary figure.
/// </remarks>
[MemoryDiagnoser]
public class ReadModelTableAccessBenchmark
{
    ReadModelTableBenchmarkFixture? _fixture;

    /// <summary>
    /// Gets or sets the container name: a short one and a 60-byte one.
    /// </summary>
    [Params("orders", "orders_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public string Container { get; set; } = "orders";

    /// <summary>
    /// Seeds the fixture and warms every measured path three times.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _fixture = ReadModelTableBenchmarkFixture.Create(Container, 1, 1).GetAwaiter().GetResult();
        for (var i = 0; i < 3; i++)
        {
            _fixture.OpenScope().GetAwaiter().GetResult();
            _fixture.Find().GetAwaiter().GetResult();
            _fixture.Apply().GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Disposes the fixture.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => _fixture?.Dispose();

    /// <summary>
    /// Opens and disposes a scope through the database.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [Benchmark]
    public Task OpenScopeThroughDatabase() => _fixture!.OpenScope();

    /// <summary>
    /// Looks up a missing key through the sink.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [Benchmark]
    public Task FindThroughSink() => _fixture!.Find();

    /// <summary>
    /// Applies a one-property change through the sink.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [Benchmark]
    public Task ApplyChangeThroughSink() => _fixture!.Apply();
}
