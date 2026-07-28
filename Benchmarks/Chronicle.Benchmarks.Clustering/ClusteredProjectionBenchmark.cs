// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures how long it takes for a projection to have actually processed a burst of events, at one silo
/// and at two.
/// </summary>
/// <remarks>
/// The measured window covers appending <see cref="EventCount"/> events across <see cref="EventSourceCount"/>
/// event sources and then waiting until the projection's own observer reports having handled up to the tail
/// those appends produced. Appends return before observers process, so closing the window on the observer's
/// last handled sequence number is what makes this a projection measurement rather than an append
/// measurement. The wait polls the observer state every 50 ms, which is the granularity floor of the result.
/// The reported mean is per projected event.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 15, invocationCount: 1)]
public class ClusteredProjectionBenchmark : IAsyncDisposable
{
    const int EventSourceCount = 20;
    const int EventsPerEventSource = 25;
    const int EventCount = EventSourceCount * EventsPerEventSource;

    static readonly TimeSpan _timeout = TimeSpan.FromMinutes(3);
    static readonly TimeSpan _setupTimeout = TimeSpan.FromSeconds(60);

    ClusterBenchmarkFixture? _fixture;
    IProjectionHandler? _handler;
    EventSourceId[] _eventSourceIds = [];

    /// <summary>
    /// Gets or sets the <see cref="ClusterTopology"/> the workload runs against.
    /// </summary>
    [Params(ClusterTopology.SingleSilo, ClusterTopology.TwoSilos, ClusterTopology.TwoSilosWithSplitRoles)]
    public ClusterTopology Topology { get; set; }

    /// <summary>
    /// Brings up the cluster and confirms the projection under measurement really is subscribed.
    /// </summary>
    /// <returns>A task that completes when the cluster is operational.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ClusterBenchmarkFixture(Topology);
        await _fixture.Start();

        var projections = _fixture.EventStore1.Projections;
        _handler = projections.GetHandlerFor<ProjectionThroughputProjection>();
        await _handler.WaitTillActive(_setupTimeout);
        await _fixture.Readiness.WaitForSubscribedObserver(projections.GetProjectionIdFor<ProjectionThroughputProjection>(), _setupTimeout);
    }

    /// <summary>
    /// Gives every iteration a fresh set of event sources, so each iteration projects into new read model
    /// instances rather than re-updating the ones the previous iteration created, and settles the cluster
    /// so no iteration measures work the previous one left in flight.
    /// </summary>
    [IterationSetup]
    public void PrepareIteration()
    {
        _eventSourceIds = [.. Enumerable.Range(0, EventSourceCount).Select(_ => EventSourceId.New())];
        _fixture?.Readiness.WaitForNoJobsInFlight(_setupTimeout).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Appends the burst and waits until the projection has handled all of it.
    /// </summary>
    /// <returns>A task that completes when the projection has caught up with the burst.</returns>
    [Benchmark(OperationsPerInvoke = EventCount)]
    public async Task ProjectEvents()
    {
        var appendResults = await Task.WhenAll(_eventSourceIds.Select(eventSourceId => _fixture!.EventStore1.EventLog.AppendMany(
            eventSourceId,
            Enumerable.Range(0, EventsPerEventSource).Select(object (index) => new ProjectedEvent($"Test{index}", index)))));

        var tail = new EventSequenceNumber(appendResults.Max(appendResult => appendResult.TailSequenceNumber.Value));
        await _handler!.WaitTillReachesEventSequenceNumber(tail, _timeout);
    }

    /// <summary>
    /// Tears the cluster down.
    /// </summary>
    /// <returns>A task that completes when the cluster is gone.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <summary>
    /// Disposes the cluster brought up for the current topology.
    /// </summary>
    /// <returns>A value task that completes when the cluster is gone.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
            _fixture = null;
        }

        GC.SuppressFinalize(this);
    }
}
