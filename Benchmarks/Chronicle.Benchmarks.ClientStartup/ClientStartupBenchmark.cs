// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;
using NullLoggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Measures repeated client preparation for an equivalent, already warmed application catalog.
/// </summary>
[MemoryDiagnoser]
public class ClientStartupBenchmark
{
    readonly EventAndContext[] _events = [new(new CatalogRecorded(), EventContext.Empty)];
    readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    InMemoryClientArtifactsProvider _catalog = null!;
    Defaults _defaults = null!;
    EventStoreForTesting _store = null!;
    ReducerInvoker _invoker = null!;
    Func<ReadModelScenario<CatalogModel<CatalogRecorded>>> _sharedScenario = null!;

    /// <summary>
    /// Gets or sets the catalog size, including the application-sized case.
    /// </summary>
    [Params(128, 1024)]
    public int EventCount { get; set; }

    /// <summary>
    /// Warms shared metadata and verifies that the catalog and live invocation are real.
    /// </summary>
    /// <exception cref="InvalidOperationException">The fixture does not prepare or dispatch the expected workload.</exception>
    [GlobalSetup]
    public void Setup()
    {
        _catalog = StartupCatalog.Create(EventCount);
        _defaults = new Defaults(_catalog);
        _store = (EventStoreForTesting)_defaults.EventStore;
        _invoker = new ReducerInvoker(_store.EventTypes, new ClientArtifactsActivator(_services, NullLoggerFactory.Instance), typeof(CatalogReducer<CatalogRecorded>), typeof(CatalogModel<CatalogRecorded>), "catalog-model");
        _sharedScenario = SharedScenarioFactory();
        VerifyFixture();
        DiscoverAndSeed();
        FreshScenarioSetup();
        SharedStoreScenarioSetup();
    }

    /// <summary>
    /// Disposes the root store after measurement.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _store.Dispose();
        _services.Dispose();
    }

    /// <summary>
    /// Discovers reducers and seeds event definitions into a fresh store for the same catalog.
    /// </summary>
    /// <returns>The number of registered event types.</returns>
    [Benchmark]
    public int DiscoverAndSeed()
    {
        using var store = new EventStoreForTesting(null, _catalog);
        return store.EventTypes.All.Count;
    }

    /// <summary>
    /// Invokes the live reducer dispatch path for exactly one event.
    /// </summary>
    /// <returns>The reduction result.</returns>
    [Benchmark]
    public Task<ReduceResult> InvokeOneEvent() => _invoker.Invoke(_services, _events, null);

    /// <summary>
    /// Creates a fresh scenario and forces its otherwise lazy store setup.
    /// </summary>
    /// <returns>The scenario read-model surface.</returns>
    [Benchmark]
    public IReadModels FreshScenarioSetup()
    {
        using var scenario = new ReadModelScenario<CatalogModel<CatalogRecorded>>(null, _defaults);
        return scenario.ReadModels;
    }

    /// <summary>
    /// Uses borrowed setup when available, or the baseline fresh-store API before borrowing existed.
    /// </summary>
    /// <returns>The scenario read-model surface.</returns>
    [Benchmark]
    public IReadModels SharedStoreScenarioSetup()
    {
        using var scenario = _sharedScenario();
        return scenario.ReadModels;
    }

    /// <summary>
    /// Creates a fresh scenario and verifies materialization, not merely lazy setup.
    /// </summary>
    /// <returns>The number of folded events.</returns>
    [Benchmark]
    public async Task<int> FreshReadModelMaterialization()
    {
        using var scenario = new ReadModelScenario<CatalogModel<CatalogRecorded>>(null, _defaults);
        await scenario.Given.ForEventSource("catalog-source").Events(new CatalogRecorded());
        return scenario.Instance?.Count == 1 ? 1 : throw new InvalidOperationException("Expected one materialized event.");
    }

    /// <summary>
    /// Creates a fresh reactor scenario, delivers an event and releases its owned store and provider.
    /// </summary>
    /// <returns>The number of delivered events.</returns>
    [Benchmark]
    public async Task<int> FreshReactorDelivery()
    {
        var probe = new CatalogDeliveryProbe();
        using var scenario = new ReactorScenario<CatalogReactor>(_defaults);
        scenario.Services.AddSingleton(probe);
        await scenario.Given.ForEventSource("catalog-source").Events(new CatalogRecorded());
        return probe.Deliveries == 1 ? 1 : throw new InvalidOperationException("Expected one reactor delivery.");
    }

    void VerifyFixture()
    {
        if (_store.EventTypes.All.Count != EventCount) throw new InvalidOperationException("Unexpected registered event count.");
        var result = InvokeOneEvent().GetAwaiter().GetResult();
        if (!result.IsSuccess) throw new InvalidOperationException("Reducer dispatch failed.");
        if (result.ReadModelState is not CatalogModel<CatalogRecorded> { Count: 1 }) throw new InvalidOperationException("Expected one folded event.");
    }

    Func<ReadModelScenario<CatalogModel<CatalogRecorded>>> SharedScenarioFactory()
    {
        // Bind once outside timing so identical benchmark source compiles before the borrowing API existed.
        var method = typeof(EventStoreForTesting).GetMethods().SingleOrDefault(method => method.Name == "CreateReadModelScenario" && method.IsGenericMethodDefinition);
        if (method is null) return () => new ReadModelScenario<CatalogModel<CatalogRecorded>>(null, _defaults);
        var factory = method.MakeGenericMethod(typeof(CatalogModel<CatalogRecorded>))
            .CreateDelegate<Func<IServiceProvider?, ReadModelScenario<CatalogModel<CatalogRecorded>>>>(_store);
        return () => factory(null);
    }
}
