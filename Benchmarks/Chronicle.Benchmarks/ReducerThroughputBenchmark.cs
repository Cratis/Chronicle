// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures the cost of driving events all the way through a reducer into its read model.
/// </summary>
/// <remarks>
/// <para>
/// The measured window covers the append and the reducer actually reducing the events into the read model, because
/// it only ends once every observer affected by the append reports having handled up to the appended tail. The only
/// observer registered is <see cref="BenchmarkReducer"/>.
/// </para>
/// <para>
/// Two costs are part of the window and have to be read out of the numbers rather than attributed to the reducer.
/// The append itself is included, so compare against <see cref="AppendManyBenchmark"/> for the same event count to
/// isolate the reducer. Completion is observed by the kernel polling every 50 ms, which puts a floor of roughly
/// 60 to 70 ms on every measurement here regardless of how little work the reducer does.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 10, invocationCount: 1)]
public class ReducerThroughputBenchmark : IDisposable, IAsyncDisposable
{
    static readonly TimeSpan _completionTimeout = TimeSpan.FromMinutes(10);

    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;

    /// <summary>
    /// Gets or sets the number of events reduced in each benchmark invocation.
    /// </summary>
    [Params(10, 100, 1000)]
    public int EventCount { get; set; }

    /// <summary>
    /// Initializes the benchmark fixture and Chronicle client.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(
            _fixture,
            $"reducer-throughput-{EventCount}",
            new ObserverScopedClientArtifactsProvider(typeof(BenchmarkReducer)));
        await _helper.WaitForConnection();
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure after the run completes.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <summary>
    /// Appends a batch of events and waits for the reducer to have reduced them.
    /// </summary>
    /// <returns>A task that completes when the reducer has caught up with the appended events.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the reducer ended up with failed partitions.</exception>
    [Benchmark]
    public async Task ReduceEvents()
    {
        var eventSourceId = EventSourceId.New();
        var events = Enumerable.Range(0, EventCount)
            .Select(i => new BenchmarkEvent(eventSourceId.Value, i, DateTimeOffset.UtcNow))
            .Cast<object>()
            .ToList();

        var appendResult = await _helper!.EventLog.AppendMany(eventSourceId, events);
        var completion = await appendResult.WaitForCompletion(_completionTimeout);
        if (!completion.IsSuccess)
        {
            throw new InvalidOperationException($"The reducer failed for {completion.FailedPartitions.Count()} partition(s).");
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
}
