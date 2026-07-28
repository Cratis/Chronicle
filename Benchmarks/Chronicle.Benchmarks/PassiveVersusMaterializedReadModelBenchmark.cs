// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Contrasts resolving an instance of a passive read model against resolving an instance of a materialized one.
/// </summary>
/// <remarks>
/// <para>
/// A passive projection never subscribes an observer and registers without a sink, so resolving an instance by key
/// projects the events of that key on demand. A materialized projection observes the event sequence and writes to its
/// sink, so resolving an instance by key reads the already projected document.
/// </para>
/// <para>
/// Both read models are fed by the same seeded events in <see cref="Setup"/>, outside the measured window, and the
/// materialized one is waited for so it is caught up before anything is measured.
/// </para>
/// <para>
/// The on-demand projection keeps the state it computed for a key, so resolving the same key twice stops projecting
/// and starts returning that state. Every invocation therefore resolves a key of its own out of a seeded pool, and
/// the job pins the invocation count to one so the number of invocations stays well inside that pool.
/// </para>
/// <para>
/// That pool holds <see cref="KeyCount"/> keys against roughly a dozen invocations at the configured warmup and
/// iteration counts. Raising the iteration count past about sixty wraps the pool around, at which point the passive
/// numbers quietly turn into measurements of cached on-demand state instead of on-demand projection. Grow the pool
/// alongside the iteration count.
/// </para>
/// <para>
/// Resolving a key the on-demand projection has not seen activates a grain for it, so grain activation is part of
/// the passive measurement. That is inherent to resolving a key that is not already in play, not overhead added by
/// the benchmark.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 10, invocationCount: 1)]
public class PassiveVersusMaterializedReadModelBenchmark : IDisposable, IAsyncDisposable
{
    const int KeyCount = 64;
    static readonly TimeSpan _completionTimeout = TimeSpan.FromMinutes(10);

    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    Guid[] _instanceIds = [];
    int _nextInstanceIndex;

    /// <summary>
    /// Gets or sets the number of events making up each resolved read model instance.
    /// </summary>
    [Params(10, 100)]
    public int EventCount { get; set; }

    /// <summary>
    /// Initializes the benchmark fixture, seeds the events and waits for the materialized read model to be projected.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    /// <exception cref="InvalidOperationException">Thrown when seeding ended up with failed partitions.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(
            _fixture,
            $"passive-versus-materialized-{EventCount}",
            new ObserverScopedClientArtifactsProvider(typeof(BenchmarkMaterializedReadModel), typeof(BenchmarkPassiveReadModel)));
        await _helper.WaitForConnection();

        _instanceIds = [.. Enumerable.Range(0, KeyCount).Select(_ => Guid.NewGuid())];
        _nextInstanceIndex = 0;

        foreach (var instanceId in _instanceIds)
        {
            var events = Enumerable.Range(0, EventCount)
                .Select(i => new BenchmarkInstanceRecorded($"Benchmark{i}", i, DateTimeOffset.UtcNow))
                .Cast<object>()
                .ToList();

            var appendResult = await _helper.EventLog.AppendMany(instanceId, events);
            var completion = await appendResult.WaitForCompletion(_completionTimeout);
            if (!completion.IsSuccess)
            {
                throw new InvalidOperationException($"Seeding failed for {completion.FailedPartitions.Count()} partition(s).");
            }
        }
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure after the run completes.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <summary>
    /// Resolves a materialized read model instance from its sink.
    /// </summary>
    /// <returns>The resolved <see cref="BenchmarkMaterializedReadModel"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no instance could be resolved.</exception>
    [Benchmark(Baseline = true)]
    public async Task<BenchmarkMaterializedReadModel> GetMaterializedInstance()
    {
        var instance = await _helper!.EventStore.ReadModels.GetInstanceById<BenchmarkMaterializedReadModel>(NextInstanceId());
        return instance ?? throw new InvalidOperationException("The materialized read model instance was not found.");
    }

    /// <summary>
    /// Resolves a passive read model instance by projecting its events on demand.
    /// </summary>
    /// <returns>The resolved <see cref="BenchmarkPassiveReadModel"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no instance could be resolved.</exception>
    [Benchmark]
    public async Task<BenchmarkPassiveReadModel> GetPassiveInstance()
    {
        var instance = await _helper!.EventStore.ReadModels.GetInstanceById<BenchmarkPassiveReadModel>(NextInstanceId());
        return instance ?? throw new InvalidOperationException("The passive read model instance was not found.");
    }

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously disposes the benchmark resources.
    /// </summary>
    /// <returns>A value task that completes when all resources have been disposed.</returns>
    public async ValueTask DisposeAsync()
    {
        _helper?.Dispose();
        _helper = null;

        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
            _fixture = null;
        }

        GC.SuppressFinalize(this);
    }

    Guid NextInstanceId()
    {
        var instanceId = _instanceIds[_nextInstanceIndex % _instanceIds.Length];
        _nextInstanceIndex++;
        return instanceId;
    }
}
