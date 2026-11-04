// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Compares detached decision reads with materialized and fresh-session passive reads as history grows.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5, invocationCount: 1)]
public class DecisionReadBenchmark : IDisposable, IAsyncDisposable
{
    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    IDecisionReads _reads = null!;
    Guid _key;

    /// <summary>
    /// Gets or sets the number of events in the key's history.
    /// </summary>
    [Params(10, 100, 1000, 10000)]
    public int HistoryLength { get; set; }

    /// <summary>
    /// Seeds the history and validates all three read paths outside the measured window.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    /// <exception cref="InvalidOperationException">Thrown when seeding or read validation fails.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(
            _fixture,
            $"decision-reads-{HistoryLength}",
            new ObserverScopedClientArtifactsProvider(typeof(BenchmarkMaterializedReadModel), typeof(BenchmarkPassiveReadModel)));
        await _helper.WaitForConnection();
        _reads = _helper.EventStore.GetDecisionReads();
        _key = Guid.NewGuid();
        await DecisionBenchmarkSeeding.Seed(_helper.EventLog, _key, HistoryLength);
        await DecisionBenchmarkSeeding.Validate(_helper.EventStore, _reads, _key, HistoryLength - 1);
        await PassiveRead();
    }

    /// <summary>
    /// Reads the already materialized instance from its sink.
    /// </summary>
    /// <returns>The validated materialized instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the read does not reflect the seeded history.</exception>
    [Benchmark(Baseline = true)]
    public async Task<BenchmarkMaterializedReadModel> MaterializedRead()
    {
        var instance = await _helper!.EventStore.ReadModels.GetInstanceById<BenchmarkMaterializedReadModel>(_key);
        if (instance is null || instance.Value != HistoryLength - 1)
        {
            throw new InvalidOperationException($"The materialized read resolved value {instance?.Value}, expected {HistoryLength - 1}.");
        }

        return instance;
    }

    /// <summary>
    /// Folds the passive model's entire history under a fresh session.
    /// </summary>
    /// <returns>The validated passive instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the read does not reflect the seeded history.</exception>
    [Benchmark]
    public async Task<BenchmarkPassiveReadModel> PassiveRead()
    {
        var instance = await _helper!.EventStore.ReadModels.GetInstanceById<BenchmarkPassiveReadModel>(_key, Guid.NewGuid());
        if (instance is null || instance.Value != HistoryLength - 1)
        {
            throw new InvalidOperationException($"The passive read resolved value {instance?.Value}, expected {HistoryLength - 1}.");
        }

        return instance;
    }

    /// <summary>
    /// Folds the history into a detached decision read without enrolling it in a unit of work.
    /// </summary>
    /// <returns>The validated decision-read instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the read does not reflect the seeded history.</exception>
    [Benchmark]
    public async Task<BenchmarkMaterializedReadModel> DecisionRead()
    {
        var read = await _reads.GetDetached<BenchmarkMaterializedReadModel>(_key);
        if (!read.Exists || read.Instance is null || read.Instance.Value != HistoryLength - 1)
        {
            throw new InvalidOperationException($"The decision read resolved value {read.Instance?.Value}, expected {HistoryLength - 1}.");
        }

        return read.Instance;
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
