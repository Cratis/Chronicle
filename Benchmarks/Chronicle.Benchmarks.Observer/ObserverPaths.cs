// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Benchmarks.Observer;

/// <summary>
/// Measures observer-side live delivery and catch-up coordination without a silo or external storage.
/// </summary>
/// <remarks>
/// Catch-up includes acquisition, partition registration, a job's handled-count report, and handover back to live delivery.
/// It does not execute the job's event reader or subscriber. Both paths use the real observer and state machine;
/// storage, queues, jobs and subscribers are test doubles. Each operation is one batch, not one event.
/// </remarks>
[MemoryDiagnoser]
public class ObserverPaths
{
    static readonly Key _partition = "benchmark-partition";
    ObserverFixture _fixture = null!;
    AppendedEvent[] _events = [];
    IReadOnlyDictionary<EventTypeId, EventCount> _counts = null!;
    ulong _next;

    /// <summary>
    /// Gets or sets the number of events represented by a batch.
    /// </summary>
    [Params(1, 100)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Creates and subscribes the in-process observer.
    /// </summary>
    /// <returns>The setup task.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ObserverFixture();
        await _fixture.Setup();
        _events = Enumerable.Range(0, BatchSize)
            .Select(index => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(ObserverFixture.EventType, (ulong)index))
            .ToArray();
        _counts = new Dictionary<EventTypeId, EventCount> { [ObserverFixture.EventType.Id] = (ulong)BatchSize };
    }

    /// <summary>
    /// Resets the live cursor outside the measured operation so the same input batch can be reused.
    /// </summary>
    [IterationSetup(Target = nameof(LiveHandling))]
    public void ResetLiveCursor() => _fixture.ResetCursor();

    /// <summary>
    /// Releases recorded substitute calls outside the measured operation.
    /// </summary>
    [IterationCleanup]
    public void ClearCalls() => _fixture.ClearCalls();

    /// <summary>
    /// Delivers a batch through Observer.Handle and verifies that it was handled, not skipped.
    /// </summary>
    /// <returns>The delivery task.</returns>
    /// <exception cref="InvalidOperationException">The batch was not delivered.</exception>
    [Benchmark]
    public async Task LiveHandling()
    {
        var before = _fixture.State.HandledEventCount.Value;
        await _fixture.Observer.Handle(_partition, _events);
        if (_fixture.State.HandledEventCount.Value != before + (ulong)BatchSize)
        {
            throw new InvalidOperationException("Live batch was not fully handled.");
        }
    }

    /// <summary>
    /// Drives one catch-up coordination round and applies the event counts reported by its job.
    /// </summary>
    /// <returns>The catch-up task.</returns>
    /// <exception cref="InvalidOperationException">The round did not complete.</exception>
    [Benchmark]
    public async Task CatchUpRound()
    {
        var before = _fixture.State.HandledEventCount.Value;
        await _fixture.Observer.CatchUp();
        await _fixture.Observer.RegisterCatchingUpPartitions([_partition]);
        _next += (ulong)BatchSize;
        await _fixture.Observer.ReportHandledEvents(_partition, _counts, JobStepId.New(), _next);
        await _fixture.Observer.CaughtUp(JobId.NotSet, _next);
        if (_fixture.State.HandledEventCount.Value != before + (ulong)BatchSize || _fixture.State.CatchingUpPartitions.Count != 0)
        {
            throw new InvalidOperationException("Catch-up round did not complete.");
        }
    }
}
