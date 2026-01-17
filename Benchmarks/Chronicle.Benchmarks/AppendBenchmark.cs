// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class AppendBenchmark
{
    ChronicleClientHelper? _helper;
    EventSourceId _eventSourceId = EventSourceId.Unspecified;

    [GlobalSetup]
    public async Task Setup()
    {
        _helper = new ChronicleClientHelper();
        await _helper.WaitForConnection();
        // Use a consistent event source ID for all iterations
        _eventSourceId = EventSourceId.New();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _helper?.Dispose();
    }

    [Benchmark]
    public async Task AppendSingleEvent()
    {
        var @event = new BenchmarkEvent("Test", 42, DateTimeOffset.UtcNow);
        await _helper!.EventLog.Append(_eventSourceId, @event);
    }
}
