// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Services;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>
/// Compares the query executor overload that does not route with the routed overload, for a non-System store.
/// </summary>
[MemoryDiagnoser]
[MedianColumn]
[MinColumn]
[MaxColumn]
[InProcess]
[WarmupCount(3)]
[IterationCount(10)]
public class QueryPathBenchmark
{
    readonly IEnumerable<int> _items = Enumerable.Range(0, 100).ToArray();
    internal readonly QueryRouting _routing = new("bench", "Default", "event-log");
    Task<IEnumerable<int>> _items100 = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the routed overload is used.
    /// </summary>
    [Params(false, true)]
    public bool Guarded { get; set; }

    /// <summary>
    /// Creates the query result the delegates return.
    /// </summary>
    [GlobalSetup]
    public void Setup() => _items100 = Task.FromResult(_items);

    /// <summary>
    /// Executes a query that returns 100 items.
    /// </summary>
    /// <returns>The query result.</returns>
    [Benchmark]
    public Task<QueryResult<IEnumerable<int>>> Query() =>
        Guarded ? QueryExecutor.Execute(default(CallContext), _routing, Read) : QueryExecutor.Execute(Read);

    Task<IEnumerable<int>> Read() => _items100;
}
