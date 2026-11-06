// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>
/// Compares the query executor overload that does not route with the routed overload, for a non-System store.
/// </summary>
[MemoryDiagnoser]
[MedianColumn]
[MinColumn]
[MaxColumn]
[Config(typeof(ColdPathConfig))]
public class QueryPathBenchmark : IDisposable
{
    readonly IEnumerable<int> _items = Enumerable.Range(0, 100).ToArray();
    readonly QueryRouting _routing = new("bench", "Default", "event-log");
    readonly QueryRouting _applicationRouting = new("bench", "Default", Capability: "chronicle.event-store.read");
    EntitledCaller _entitled = null!;
    EntitledCaller _unentitledAudited = null!;
    Task<IEnumerable<int>> _items100 = null!;
    Func<IObservable<IEnumerable<int>>> _observe = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the routed overload is used.
    /// </summary>
    [Params(false, true)]
    public bool Guarded { get; set; }

    /// <summary>
    /// Creates the query result the delegates return.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _items100 = Task.FromResult(_items);
        _observe = Observe;
        _entitled = new(true, true);
        _unentitledAudited = new(false, false);
    }

    /// <summary>
    /// Releases the snapshots.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => Dispose();

    /// <inheritdoc/>
    public void Dispose()
    {
        _entitled.Dispose();
        _unentitledAudited.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Executes a query for a caller whose Read grant is in the snapshot, with enforcement on.
    /// </summary>
    /// <returns>The query result.</returns>
    [Benchmark]
    public Task<QueryResult<IEnumerable<int>>> EntitledRead() => QueryExecutor.Execute(_entitled.NextRequest(), _applicationRouting, Read);

    /// <summary>
    /// Executes a query for a caller without a grant, with enforcement off (audited).
    /// </summary>
    /// <returns>The query result.</returns>
    [Benchmark]
    public Task<QueryResult<IEnumerable<int>>> AuditedUnentitledRead() => QueryExecutor.Execute(_unentitledAudited.NextRequest(), _applicationRouting, Read);

    /// <summary>
    /// Creates a hand-written application-store stream for an entitled caller, with enforcement on.
    /// </summary>
    [Benchmark]
    public void EntitledObservableGuard()
    {
        using var subscription = Services.SystemStoreGuard.Ensure(_entitled.NextRequest(), _applicationRouting, StoreAccess.Read, _observe).Subscribe();
    }

    /// <summary>Creates fresh request identities outside the measured operation.</summary>
    [IterationSetup]
    public void PrepareRequests()
    {
        _entitled.PrepareRequests();
        _unentitledAudited.PrepareRequests();
    }

    /// <summary>
    /// Executes a query that returns 100 items.
    /// </summary>
    /// <returns>The query result.</returns>
    [Benchmark]
    public Task<QueryResult<IEnumerable<int>>> Query() =>
        Guarded ? QueryExecutor.Execute(default, _routing, Read) : QueryExecutor.Execute(Read);

    /// <summary>
    /// Executes an observable application-store query.
    /// </summary>
    /// <returns>The query stream.</returns>
    [Benchmark]
    public IObservable<QueryResult<IEnumerable<int>>> ObservableQuery() =>
        Guarded ? QueryExecutor.Execute(default, _routing, Observe) : QueryExecutor.Execute(Observe);

    /// <summary>
    /// Creates a guarded or unguarded hand-written application-store stream with the same preallocated source delegate.
    /// </summary>
    /// <returns>The stream.</returns>
    [Benchmark]
    public IObservable<IEnumerable<int>> ObservableGuard() =>
        Guarded ? Services.SystemStoreGuard.Ensure(default, _routing, StoreAccess.Read, _observe) : _observe();

    IObservable<IEnumerable<int>> Observe() => Observable.Return(_items);

    Task<IEnumerable<int>> Read() => _items100;
}
