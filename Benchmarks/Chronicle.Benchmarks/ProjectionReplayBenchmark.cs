// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures the cost of reconstructing a projection from scratch by replaying an already seeded event sequence.
/// </summary>
/// <remarks>
/// <para>
/// The event sequence is seeded in <see cref="Setup"/>, outside the measured window. The measured window covers
/// triggering a replay through <see cref="Projections.IProjections.Replay{TProjection}"/> and waiting until the
/// observer has reached the seeded tail again.
/// </para>
/// <para>
/// Entering the replay state is awaited by the replay call itself, so the observer is guaranteed to report
/// <see cref="ObserverRunningState.Replaying"/> by the time the measured window starts polling for it to be caught
/// up again. Caught up means the observer is back to <see cref="ObserverRunningState.Active"/> and has handled up to
/// the seeded tail.
/// </para>
/// <para>
/// Being caught up is observed by polling the observer state every 25 ms, so up to that much of each measurement is
/// polling granularity rather than replay work. The append is not part of the window - seeding happens entirely in
/// <see cref="Setup"/>.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5, invocationCount: 1)]
public class ProjectionReplayBenchmark : IDisposable, IAsyncDisposable
{
    const int SeedBatchSize = 500;
    const int CaughtUpPollingDelayMs = 25;
    static readonly TimeSpan _completionTimeout = TimeSpan.FromMinutes(10);

    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    EventSequenceNumber _seededTail = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the number of events the projection is reconstructed from.
    /// </summary>
    [Params(1000, 10000)]
    public int EventCount { get; set; }

    /// <summary>
    /// Initializes the benchmark fixture, seeds the event sequence and waits for the projection to have caught up.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    /// <exception cref="InvalidOperationException">Thrown when seeding ended up with failed partitions.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(
            _fixture,
            $"projection-replay-{EventCount}",
            new ObserverScopedClientArtifactsProvider(typeof(BenchmarkProjection)));
        await _helper.WaitForConnection();

        var eventSourceId = EventSourceId.New();
        for (var seeded = 0; seeded < EventCount; seeded += SeedBatchSize)
        {
            var batchSize = Math.Min(SeedBatchSize, EventCount - seeded);
            var events = Enumerable.Range(seeded, batchSize)
                .Select(i => new BenchmarkEvent(eventSourceId.Value, i, DateTimeOffset.UtcNow))
                .Cast<object>()
                .ToList();

            var appendResult = await _helper.EventLog.AppendMany(eventSourceId, events);
            var completion = await appendResult.WaitForCompletion(_completionTimeout);
            if (!completion.IsSuccess)
            {
                throw new InvalidOperationException($"Seeding failed for {completion.FailedPartitions.Count()} partition(s).");
            }
        }

        _seededTail = await _helper.EventLog.GetTailSequenceNumber();
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure after the run completes.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <summary>
    /// Replays the projection over the seeded event sequence and waits until it has caught up again.
    /// </summary>
    /// <returns>A task that completes when the projection has been reconstructed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no replay job was started.</exception>
    [Benchmark]
    public async Task ReplayProjection()
    {
        var jobId = await _helper!.EventStore.Projections.Replay<BenchmarkProjection>();
        if (jobId == JobId.NotSet)
        {
            throw new InvalidOperationException("The projection did not start a replay job and is likely not replayable.");
        }

        await WaitUntilCaughtUp();
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

    async Task WaitUntilCaughtUp()
    {
        var handler = _helper!.EventStore.Projections.GetHandlerFor<BenchmarkProjection>();
        using var cancellation = new CancellationTokenSource(_completionTimeout);

        while (true)
        {
            var state = await handler.GetState();
            if (state.RunningState == ObserverRunningState.Active &&
                state.LastHandledEventSequenceNumber.IsActualValue &&
                state.LastHandledEventSequenceNumber.Value >= _seededTail.Value)
            {
                return;
            }

            await Task.Delay(CaughtUpPollingDelayMs, cancellation.Token);
        }
    }
}
