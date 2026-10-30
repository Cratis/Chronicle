// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures the Kernel's append pipeline for one large batch, the shape that took a client over its
/// call deadline in Cratis/Chronicle.Elixir#188 (a single append of roughly 6000 events).
/// </summary>
/// <remarks>
/// One silo, in process, so the measurement is the Kernel append pipeline (validation, compliance,
/// constraints, storage and the post-append bookkeeping) without network or container noise. The event
/// type has no observer. Each invocation appends one batch to fresh event sources, so no iteration
/// measures an already populated stream. The reported mean is for the whole batch; divide by
/// <see cref="BatchSize"/> for the cost per event.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 10, invocationCount: 1)]
public class LargeBatchAppendBenchmark : IAsyncDisposable
{
    ClusterBenchmarkFixture? _fixture;
    object[] _events = [];
    EventForEventSourceId[] _eventsForEventSources = [];
    EventForEventSourceId[] _uniquelyNamedEvents = [];
    EventForEventSourceId[] _personalEvents = [];
    EventSourceId _eventSourceId = EventSourceId.Unspecified;

    /// <summary>
    /// Gets or sets the number of events in the batch.
    /// </summary>
    [Params(100, 1000, 6000)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Brings up a single silo.
    /// </summary>
    /// <returns>A task that completes when the silo is operational.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ClusterBenchmarkFixture(ClusterTopology.SingleSilo);
        await _fixture.Start();
    }

    /// <summary>
    /// Builds the batch outside the measured window, against fresh event sources.
    /// </summary>
    [IterationSetup]
    public void PrepareIteration()
    {
        _eventSourceId = EventSourceId.New();
        _events = [.. Enumerable.Range(0, BatchSize).Select(object (index) => new AppendOnlyEvent($"Test{index}", index))];

        // One event source per ten events, the shape of an import that appends for many sources at once.
        var eventSourceIds = Enumerable.Range(0, Math.Max(1, BatchSize / 10)).Select(_ => EventSourceId.New()).ToArray();
        _eventsForEventSources = [.. Enumerable.Range(0, BatchSize)
            .Select(index => new EventForEventSourceId(eventSourceIds[index % eventSourceIds.Length], new AppendOnlyEvent($"Test{index}", index)))];
        _uniquelyNamedEvents = [.. Enumerable.Range(0, BatchSize)
            .Select(index => new EventForEventSourceId(eventSourceIds[index % eventSourceIds.Length], new UniquelyNamedEvent(Guid.NewGuid().ToString(), index)))];
        _personalEvents = [.. Enumerable.Range(0, BatchSize)
            .Select(index => new EventForEventSourceId(eventSourceIds[index % eventSourceIds.Length], new PersonRegistered($"Person{index}", index)))];
    }

    /// <summary>
    /// Appends the batch for a single event source.
    /// </summary>
    /// <returns>A task that completes when the batch is appended.</returns>
    [Benchmark(Baseline = true)]
    public async Task AppendManyForOneEventSource()
    {
        var result = await _fixture!.EventStore1.EventLog.AppendMany(_eventSourceId, _events);
        EnsureSucceeded(result);
    }

    /// <summary>
    /// Appends the batch spread over many event sources.
    /// </summary>
    /// <returns>A task that completes when the batch is appended.</returns>
    [Benchmark]
    public async Task AppendManyForManyEventSources()
    {
        var result = await _fixture!.EventStore1.EventLog.AppendMany(_eventsForEventSources);
        EnsureSucceeded(result);
    }

    /// <summary>
    /// Appends the batch spread over many event sources, for an event type carrying a unique constraint.
    /// </summary>
    /// <returns>A task that completes when the batch is appended.</returns>
    [Benchmark]
    public async Task AppendManyWithUniqueConstraint()
    {
        var result = await _fixture!.EventStore1.EventLog.AppendMany(_uniquelyNamedEvents);
        EnsureSucceeded(result);
    }

    /// <summary>
    /// Appends the batch spread over many event sources, for an event type carrying personal data, so every event
    /// source is a subject needing an encryption key.
    /// </summary>
    /// <returns>A task that completes when the batch is appended.</returns>
    [Benchmark]
    public async Task AppendManyWithPersonalData()
    {
        var result = await _fixture!.EventStore1.EventLog.AppendMany(_personalEvents);
        EnsureSucceeded(result);
    }

    /// <summary>
    /// Tears the silo down.
    /// </summary>
    /// <returns>A task that completes when the silo is gone.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
            _fixture = null;
        }

        GC.SuppressFinalize(this);
    }

    void EnsureSucceeded(AppendManyResult result)
    {
        // A failed append returns quickly and would report a meaningless, flattering number.
        if (!result.IsSuccess || result.SequenceNumbers.Count() != BatchSize)
        {
            throw new InvalidOperationException($"Append of {BatchSize} events did not succeed: {string.Join(", ", result.Errors.Select(_ => _.ToString()))}");
        }
    }
}
