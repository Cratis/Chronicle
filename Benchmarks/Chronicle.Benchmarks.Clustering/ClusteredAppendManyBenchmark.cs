// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures the cost of appending events in batches, at one silo and at two.
/// </summary>
/// <remarks>
/// The measured window covers <see cref="BatchCount"/> concurrent batch appends of <see cref="BatchSize"/>
/// events each, of an event type no observer subscribes to, so it is the append path alone. The reported
/// mean is per appended event.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 15, invocationCount: 1)]
public class ClusteredAppendManyBenchmark : IAsyncDisposable
{
    const int BatchCount = 10;
    const int BatchSize = 50;
    const int EventCount = BatchCount * BatchSize;

    ClusterBenchmarkFixture? _fixture;
    EventSourceId[] _eventSourceIds = [];

    /// <summary>
    /// Gets or sets the <see cref="ClusterTopology"/> the workload runs against.
    /// </summary>
    [Params(ClusterTopology.SingleSilo, ClusterTopology.TwoSilos, ClusterTopology.TwoSilosWithSplitRoles)]
    public ClusterTopology Topology { get; set; }

    /// <summary>
    /// Brings up the cluster for the current topology.
    /// </summary>
    /// <returns>A task that completes when the cluster is operational.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ClusterBenchmarkFixture(Topology);
        await _fixture.Start();
    }

    /// <summary>
    /// Gives every iteration a fresh set of event sources, so no iteration measures an already populated stream.
    /// </summary>
    [IterationSetup]
    public void PrepareIteration() => _eventSourceIds = [.. Enumerable.Range(0, BatchCount).Select(_ => EventSourceId.New())];

    /// <summary>
    /// Appends every batch concurrently.
    /// </summary>
    /// <returns>A task that completes when every batch has been appended.</returns>
    [Benchmark(OperationsPerInvoke = EventCount)]
    public Task AppendManyEvents() =>
        Task.WhenAll(_eventSourceIds.Select(eventSourceId => _fixture!.EventStore1.EventLog.AppendMany(
            eventSourceId,
            Enumerable.Range(0, BatchSize).Select(object (index) => new AppendOnlyEvent($"Test{index}", index)))));

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
