// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// The storage reads an event sequence performs when it activates: its saved state and its tail sequence numbers.
/// This measures storage reads on every provider; warm/cold/post-crash grain activation is tracked separately in #6045.
/// </summary>
[MemoryDiagnoser]
public class ActivationStorageBenchmark : StorageBenchmark
{
    IEventSequenceStorage _storage = null!;

    /// <summary>Starts the host and seeds a sequence outside measurement.</summary>
    /// <returns>The task.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        await StartHost();
        _storage = Host.CreateSequence();
        var count = Profile.EventCounts.Min();
        await Seed(_storage, count);
        await _storage.SaveState(new EventSequenceState
        {
            SequenceNumber = (ulong)count,
            TailSequenceNumberPerEventType = (await _storage.GetTailSequenceNumbersForEventTypes([BenchmarkEvents.Selected, BenchmarkEvents.Other])).ToDictionary(pair => pair.Key.Id, pair => pair.Value)
        });
        Host.Counter.Reset();
    }

    /// <summary>Reads the sequence state.</summary>
    /// <returns>The task.</returns>
    [Benchmark]
    public Task GetState() => Measure(Key("activation-get-state"), _ => 1, async () => await _storage.GetState());

    /// <summary>Reads the tail sequence number.</summary>
    /// <returns>The task.</returns>
    [Benchmark]
    public Task GetTail() => Measure(Key("activation-get-tail"), _ => 1, async () => (ulong)await _storage.GetTailSequenceNumber());

    /// <summary>Reads the tail sequence number per event type.</summary>
    /// <returns>The task.</returns>
    [Benchmark]
    public Task GetTailPerEventType() => Measure(Key("activation-get-tail-per-type"), _ => 1, async () => await _storage.GetTailSequenceNumbersForEventTypes([BenchmarkEvents.Selected, BenchmarkEvents.Other]));
}
