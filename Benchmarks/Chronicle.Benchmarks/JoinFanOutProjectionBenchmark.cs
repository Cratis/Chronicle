// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures the cost of the applied event markers that make redelivery a no-op for event-source-keyed read models
/// with root joins (Cratis/Chronicle#5524).
/// </summary>
/// <remarks>
/// <para>
/// Run it against the base commit and against the change, with the same MongoDB sink, and compare. The budgets are
/// at most +15% for <see cref="FanOutJoin"/> and at most +10% for <see cref="CatchUp"/>. Twice the budget is a stop.
/// </para>
/// <para>
/// <see cref="FanOutJoin"/> appends one join event that is written to every order of the customer, seeded in
/// <see cref="Setup"/>. <see cref="CatchUp"/> appends a large run of own events, which is the bulk catch-up path.
/// Clearing the markers of a partition on a large read model is not benchmarked here because a partition replay has no
/// client entry point; it is exercised through the sink specifications.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5, invocationCount: 1)]
public class JoinFanOutProjectionBenchmark : IDisposable, IAsyncDisposable
{
    const int FanOutDocuments = 1000;
    const int CatchUpEvents = 50000;
    const int AppendBatchSize = 5000;
    static readonly TimeSpan _completionTimeout = TimeSpan.FromMinutes(10);

    readonly string _customerId = Guid.NewGuid().ToString();
    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    int _renames;

    /// <summary>
    /// Initializes the benchmark fixture and seeds the orders of one customer, outside the measured window.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(
            _fixture,
            "join-fan-out",
            new ObserverScopedClientArtifactsProvider(typeof(BenchmarkJoinProjection)));
        await _helper.WaitForConnection();

        await AppendAndWait(Enumerable.Range(0, FanOutDocuments)
            .Select(i => new EventForEventSourceId(EventSourceId.New(), new BenchmarkJoinOrderPlaced(_customerId, i)))
            .ToList());
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure after the run completes.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <summary>
    /// Appends one join event and waits for it to be written to every order of the customer.
    /// </summary>
    /// <returns>A task that completes when the projection has caught up.</returns>
    [Benchmark]
    public Task FanOutJoin() => AppendAndWait(
        [new EventForEventSourceId((EventSourceId)_customerId, new BenchmarkJoinCustomerRenamed($"name-{++_renames}"))]);

    /// <summary>
    /// Appends a large run of own events, each for its own order, and waits for the projection to have handled them.
    /// </summary>
    /// <returns>A task that completes when the projection has caught up.</returns>
    [Benchmark]
    public async Task CatchUp()
    {
        for (var appended = 0; appended < CatchUpEvents; appended += AppendBatchSize)
        {
            await AppendAndWait(Enumerable.Range(0, AppendBatchSize)
                .Select(i => new EventForEventSourceId(EventSourceId.New(), new BenchmarkJoinOrderPlaced(_customerId, i)))
                .ToList());
        }
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

    async Task AppendAndWait(IReadOnlyList<EventForEventSourceId> events)
    {
        var appendResult = await _helper!.EventLog.AppendMany(events);
        var completion = await appendResult.WaitForCompletion(_completionTimeout);
        if (!completion.IsSuccess)
        {
            throw new InvalidOperationException($"The projection failed for {completion.FailedPartitions.Count()} partition(s).");
        }
    }
}
