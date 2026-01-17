// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class AppendManyBenchmark
{
    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;

    [Params(10, 100, 1000)]
    public int EventCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(_fixture);
        await _helper.WaitForConnection();
        // Use a consistent event source ID for all iterations
        _eventSourceId = EventSourceId.New();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _helper?.Dispose();
        if (_fixture != null)
        {
            await _fixture.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task AppendManyEvents()
    {
        var events = Enumerable.Range(0, EventCount)
            .Select(i => new BenchmarkEvent($"Test{i}", i, DateTimeOffset.UtcNow))
            .Cast<object>()
            .ToList();

        await _helper!.EventLog.AppendMany(_eventSourceId, events);
    }
}
