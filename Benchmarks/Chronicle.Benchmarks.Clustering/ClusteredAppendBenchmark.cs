// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures the cost of appending single events, at one silo and at two.
/// </summary>
/// <remarks>
/// The measured window covers <see cref="EventCount"/> sequential appends of an event type no observer
/// subscribes to, so it is the append path alone. The reported mean is per appended event.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 15, invocationCount: 1)]
public class ClusteredAppendBenchmark : IAsyncDisposable
{
    const int EventCount = 100;

    ClusterBenchmarkFixture? _fixture;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;

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
    /// Gives every iteration a fresh event source, so no iteration measures an already populated stream.
    /// </summary>
    [IterationSetup]
    public void PrepareIteration() => _eventSourceId = EventSourceId.New();

    /// <summary>
    /// Appends events one at a time.
    /// </summary>
    /// <returns>A task that completes when every event has been appended.</returns>
    [Benchmark(OperationsPerInvoke = EventCount)]
    public async Task AppendSingleEvents()
    {
        for (var index = 0; index < EventCount; index++)
        {
            await _fixture!.EventStore1.EventLog.Append(_eventSourceId, new AppendOnlyEvent($"Test{index}", index));
        }
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
