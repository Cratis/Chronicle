// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures what a strongly consistent read of one key costs as the key's event stream grows.
/// </summary>
/// <remarks>
/// <para>
/// Two backings are measured. A passive projection is folded in the Kernel by an immediate projection grain, which
/// keeps the state it folded and the last sequence number it handled. A passive reducer is folded in the client, which
/// fetches every event of the key and reduces them on every read.
/// </para>
/// <para>
/// The projection is read in four states. Cold resolves the key under a fresh session, which activates a grain of its
/// own and folds the whole stream. Warm reads the grain the setup primed, with nothing appended since. Warm after an
/// own append appends one event to the measured key before the iteration, so the grain folds that one event. Warm after
/// another append appends one event to a different key before the iteration, which is what a busy store looks like: the
/// grain's shortcut compares against the tail of the event types across the whole sequence, not of the key.
/// </para>
/// <para>
/// Each appended event lengthens the measured stream by one. At the configured iteration counts that is a handful of
/// events against a stream of at least ten, so it is noise rather than drift.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5, invocationCount: 1)]
public class StronglyConsistentPerKeyReadBenchmark : IDisposable, IAsyncDisposable
{
    const int SeedBatchSize = 1000;
    static readonly TimeSpan _completionTimeout = TimeSpan.FromMinutes(10);

    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    Guid _key;
    Guid _otherKey;
    int _nextValue;
    int _expectedValue;
    int _projectedValue;

    /// <summary>
    /// Gets or sets the number of events in the measured key's stream.
    /// </summary>
    [Params(10, 100, 1000, 10000)]
    public int StreamLength { get; set; }

    /// <summary>
    /// Initializes the benchmark fixture, seeds the stream and primes the immediate projection grain.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    /// <exception cref="InvalidOperationException">Thrown when seeding failed or a read does not reflect the seeded stream.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(
            _fixture,
            $"per-key-reads-{StreamLength}",
            new ObserverScopedClientArtifactsProvider(typeof(BenchmarkPassiveReadModel), typeof(BenchmarkPassiveReducer)));
        await _helper.WaitForConnection();

        _key = Guid.NewGuid();
        _otherKey = Guid.NewGuid();
        _nextValue = 0;

        for (var offset = 0; offset < StreamLength; offset += SeedBatchSize)
        {
            var count = Math.Min(SeedBatchSize, StreamLength - offset);
            var events = Enumerable.Range(0, count).Select(_ => NextEvent()).Cast<object>().ToList();
            var appendResult = await _helper.EventLog.AppendMany(_key, events);
            if (!appendResult.IsSuccess)
            {
                throw new InvalidOperationException("The seed batch append failed.");
            }

            var completion = await appendResult.WaitForCompletion(_completionTimeout);
            if (!completion.IsSuccess)
            {
                throw new InvalidOperationException($"Seeding failed for {completion.FailedPartitions.Count()} partition(s).");
            }
        }

        var otherAppend = await _helper.EventLog.Append(_otherKey, NextEvent());
        if (!otherAppend.IsSuccess)
        {
            throw new InvalidOperationException("The setup append to the other key failed.");
        }
        _expectedValue = StreamLength - 1;

        // Refuse to measure anything that does not reflect the whole seeded stream - a read that resolved to nothing
        // would otherwise report the cost of resolving nothing.
        var projected = await ReadProjection(null);
        if (projected.Value != StreamLength - 1)
        {
            throw new InvalidOperationException($"The passive projection resolved value {projected.Value}, expected {StreamLength - 1}.");
        }

        var reduced = await ReadReducer();
        if (reduced.EventsReduced != StreamLength)
        {
            throw new InvalidOperationException($"The passive reducer folded {reduced.EventsReduced} events, expected {StreamLength}.");
        }
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure after the run completes.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <summary>
    /// Appends one event to the measured key before an iteration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the append to the measured key fails.</exception>
    [IterationSetup(Target = nameof(ProjectionWarmAfterOwnAppend))]
    public void AppendToMeasuredKey() => _expectedValue = AppendToKey(_key, "measured");

    /// <summary>
    /// Appends one event to another key before an iteration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the append to the other key fails.</exception>
    [IterationSetup(Target = nameof(ProjectionWarmAfterOtherAppend))]
    public void AppendToOtherKey() => AppendToKey(_otherKey, "other");

    /// <summary>
    /// Verifies the value returned by the measured read outside the timed region.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the measured read does not reflect the expected value.</exception>
    [IterationCleanup(Targets = new[] { nameof(ProjectionCold), nameof(ProjectionWarm), nameof(ProjectionWarmAfterOwnAppend), nameof(ProjectionWarmAfterOtherAppend) })]
    public void ValidateProjectionValue()
    {
        if (_projectedValue != _expectedValue)
        {
            throw new InvalidOperationException($"The passive projection resolved value {_projectedValue}, expected {_expectedValue}.");
        }
    }

    /// <summary>
    /// Resolves the key under a fresh session, folding the whole stream in a newly activated grain.
    /// </summary>
    /// <returns>The resolved <see cref="BenchmarkPassiveReadModel"/>.</returns>
    [Benchmark]
    public Task<BenchmarkPassiveReadModel> ProjectionCold() => ReadProjection(Guid.NewGuid());

    /// <summary>
    /// Resolves the key from the primed grain with nothing appended since its last read.
    /// </summary>
    /// <returns>The resolved <see cref="BenchmarkPassiveReadModel"/>.</returns>
    [Benchmark]
    public Task<BenchmarkPassiveReadModel> ProjectionWarm() => ReadProjection(null);

    /// <summary>
    /// Resolves the key from the primed grain after one event was appended to it.
    /// </summary>
    /// <returns>The resolved <see cref="BenchmarkPassiveReadModel"/>.</returns>
    [Benchmark]
    public Task<BenchmarkPassiveReadModel> ProjectionWarmAfterOwnAppend() => ReadProjection(null);

    /// <summary>
    /// Resolves the key from the primed grain after one event was appended to another key.
    /// </summary>
    /// <returns>The resolved <see cref="BenchmarkPassiveReadModel"/>.</returns>
    [Benchmark]
    public Task<BenchmarkPassiveReadModel> ProjectionWarmAfterOtherAppend() => ReadProjection(null);

    /// <summary>
    /// Resolves the key through the passive reducer, which fetches and reduces the whole stream on every read.
    /// </summary>
    /// <returns>The resolved <see cref="BenchmarkPassiveReducedReadModel"/>.</returns>
    [Benchmark]
    public Task<BenchmarkPassiveReducedReadModel> ReducerRead() => ReadReducer();

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

    async Task<BenchmarkPassiveReadModel> ReadProjection(ReadModelSessionId? sessionId)
    {
        var instance = await _helper!.EventStore.ReadModels.GetInstanceById<BenchmarkPassiveReadModel>(_key, sessionId)
            ?? throw new InvalidOperationException("The passive read model instance was not found.");
        _projectedValue = instance.Value;

        return instance;
    }

    async Task<BenchmarkPassiveReducedReadModel> ReadReducer()
    {
        var instance = await _helper!.EventStore.ReadModels.GetInstanceById<BenchmarkPassiveReducedReadModel>(_key);
        return instance ?? throw new InvalidOperationException("The passive reducer read model instance was not found.");
    }

    int AppendToKey(Guid key, string keyDescription)
    {
        var @event = NextEvent();
        var result = _helper!.EventLog.Append(key, @event).GetAwaiter().GetResult();
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"The iteration append to the {keyDescription} key failed.");
        }

        return @event.Value;
    }

    BenchmarkInstanceRecorded NextEvent()
    {
        var value = _nextValue++;
        return new($"Benchmark{value}", value, DateTimeOffset.UtcNow);
    }
}
