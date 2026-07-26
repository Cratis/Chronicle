// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures how long a projection takes to be rebuilt from the event log, at one silo and at two.
/// </summary>
/// <remarks>
/// The corpus is seeded outside the measured window. The window covers triggering the replay, the replay
/// job reaching a terminal state, the projection's observer coming back to active having handled up to the
/// seeded tail again, and every job the replay spawned finishing. The last two are what make the window
/// honest: the observer's last handled sequence number never drops during a replay, so waiting on it alone
/// returns immediately, and the replay leaves a catch-up job behind that would otherwise run into the next
/// iteration. Replay is driven by job steps, which are separate grains and therefore the workload most
/// likely to actually distribute across silos. The waits poll every 50 ms, which is the granularity floor
/// of the result. The reported mean is per replayed event.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5, invocationCount: 1)]
public class ClusteredReplayBenchmark : IAsyncDisposable
{
    const int EventSourceCount = 40;
    const int EventsPerEventSource = 50;
    const int EventCount = EventSourceCount * EventsPerEventSource;

    static readonly TimeSpan _timeout = TimeSpan.FromMinutes(3);
    static readonly TimeSpan _setupTimeout = TimeSpan.FromSeconds(60);

    ClusterBenchmarkFixture? _fixture;
    IProjectionHandler? _handler;
    EventSequenceNumber _seededTail = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the <see cref="ClusterTopology"/> the workload runs against.
    /// </summary>
    [Params(ClusterTopology.SingleSilo, ClusterTopology.TwoSilos, ClusterTopology.TwoSilosWithSplitRoles)]
    public ClusterTopology Topology { get; set; }

    /// <summary>
    /// Brings up the cluster, confirms the projection is subscribed and seeds the corpus that every
    /// iteration replays.
    /// </summary>
    /// <returns>A task that completes when the corpus has been projected once.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ClusterBenchmarkFixture(Topology);
        await _fixture.Start();

        var projections = _fixture.EventStore1.Projections;
        _handler = projections.GetHandlerFor<ReplayThroughputProjection>();
        await _handler.WaitTillActive(_setupTimeout);
        await _fixture.WaitForSubscribedObserver(projections.GetProjectionIdFor<ReplayThroughputProjection>(), _setupTimeout);

        var eventSourceIds = Enumerable.Range(0, EventSourceCount).Select(_ => EventSourceId.New());
        var appendResults = await Task.WhenAll(eventSourceIds.Select(eventSourceId => _fixture.EventStore1.EventLog.AppendMany(
            eventSourceId,
            Enumerable.Range(0, EventsPerEventSource).Select(object (index) => new ReplayedEvent($"Test{index}", index)))));

        _seededTail = new EventSequenceNumber(appendResults.Max(appendResult => appendResult.TailSequenceNumber.Value));
        await _handler.WaitTillReachesEventSequenceNumber(_seededTail, _timeout);
    }

    /// <summary>
    /// Settles the cluster before the window opens, so no iteration triggers a replay into an observer that
    /// is still finishing the previous one.
    /// </summary>
    [IterationSetup]
    public void PrepareIteration()
    {
        _handler!.WaitForState(ObserverRunningState.Active, _setupTimeout).GetAwaiter().GetResult();
        _fixture!.WaitForNoJobsInFlight(_setupTimeout).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Replays the projection over the whole seeded corpus and waits until it has caught up again.
    /// </summary>
    /// <returns>A task that completes when the projection has been rebuilt.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the projection turns out not to be replayable.</exception>
    [Benchmark(OperationsPerInvoke = EventCount)]
    public async Task ReplayProjection()
    {
        var jobId = await _fixture!.EventStore1.Projections.Replay<ReplayThroughputProjection>();
        if (jobId == JobId.NotSet)
        {
            throw new InvalidOperationException("The projection is not replayable, so no replay work was measured.");
        }

        await _fixture.EventStore1.Jobs.WaitTillJobCompletesOrIsDeleted(jobId, _timeout);
        await _handler!.WaitForState(ObserverRunningState.Active, _timeout);
        await _handler!.WaitTillReachesEventSequenceNumber(_seededTail, _timeout);
        await _fixture.WaitForNoJobsInFlight(_timeout);
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
