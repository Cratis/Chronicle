// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures the cost of appending a single event to Chronicle.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class AppendBenchmark : IDisposable, IAsyncDisposable
{
    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;

    /// <summary>
    /// Initializes the benchmark fixture and Chronicle client.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(_fixture);
        await _helper.WaitForConnection();
        _eventSourceId = EventSourceId.New();
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure after the run completes.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <summary>
    /// Appends a single event to the benchmark event log.
    /// </summary>
    /// <returns>A task that completes when the event has been appended.</returns>
    [Benchmark]
    public async Task AppendSingleEvent()
    {
        var @event = new BenchmarkEvent("Test", 42, DateTimeOffset.UtcNow);
        await _helper!.EventLog.Append(_eventSourceId, @event);
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
