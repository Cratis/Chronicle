// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures concurrent detached decision reads of one model type across distinct keys.
/// </summary>
/// <remarks>
/// Time is per batch of Workers reads, not per read. This exposes the shared-projection-grain signal for #4854.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5, invocationCount: 1)]
public class ConcurrentDecisionReadBenchmark : IDisposable, IAsyncDisposable
{
    const int HistoryLength = 100;
    const int KeyCount = 32;
    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    IDecisionReads _reads = null!;
    Guid[] _keys = [];
    DecisionRead<BenchmarkMaterializedReadModel>[] _results = [];

    /// <summary>
    /// Gets or sets the number of concurrent reads in a measured batch.
    /// </summary>
    [Params(1, 8, 32)]
    public int Workers { get; set; }

    /// <summary>
    /// Seeds all keys and validates materialized and detached decision reads for each key.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    /// <exception cref="InvalidOperationException">Thrown when seeding or read validation fails.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(
            _fixture,
            $"concurrent-decision-reads-{Workers}",
            new ObserverScopedClientArtifactsProvider(typeof(BenchmarkMaterializedReadModel)));
        await _helper.WaitForConnection();
        _reads = _helper.EventStore.GetDecisionReads();
        _keys = [.. Enumerable.Range(0, KeyCount).Select(_ => Guid.NewGuid())];
        foreach (var key in _keys)
        {
            await DecisionBenchmarkSeeding.Seed(_helper.EventLog, key, HistoryLength);
            await DecisionBenchmarkSeeding.Validate(_helper.EventStore, _reads, key, HistoryLength - 1);
        }
    }

    /// <summary>
    /// Reads a batch of distinct keys concurrently through the same model type.
    /// </summary>
    /// <returns>The detached decision reads for the batch.</returns>
    [Benchmark]
    public async Task<DecisionRead<BenchmarkMaterializedReadModel>[]> ConcurrentDecisionReads()
    {
        _results = await Task.WhenAll(_keys.Take(Workers).Select(_ => _reads.GetDetached<BenchmarkMaterializedReadModel>(_)));
        return _results;
    }

    /// <summary>
    /// Validates every measured result outside the timed region.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the batch is incomplete or a read has an unexpected value.</exception>
    [IterationCleanup]
    public void ValidateResults()
    {
        if (_results.Length != Workers || _results.Any(_ => !_.Exists || _.Instance?.Value != HistoryLength - 1))
        {
            throw new InvalidOperationException($"The batch must contain {Workers} decision reads with value {HistoryLength - 1}.");
        }
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously disposes the benchmark resources.
    /// </summary>
    /// <returns>A value task that completes when resources have been disposed.</returns>
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
}
