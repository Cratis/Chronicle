// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;

namespace Chronicle.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class AppendManyBenchmark
{
    ChronicleClientHelper? _helper;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;

    [Params(10, 100, 1000)]
    public int EventCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _helper = new ChronicleClientHelper();
        await _helper.WaitForConnection();
        _eventSourceId = Guid.NewGuid().ToString();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _helper?.Dispose();
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
