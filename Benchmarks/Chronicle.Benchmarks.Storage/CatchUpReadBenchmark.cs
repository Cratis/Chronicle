// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Cold catch-up reads over a seeded sequence: a full drain, one event type, and cursors starting deep in the
/// sequence. Reports commands per 1,000 events read.
/// </summary>
[MemoryDiagnoser]
public class CatchUpReadBenchmark : StorageBenchmark
{
    IEventSequenceStorage _storage = null!;

    /// <summary>Gets the event counts of this run, for `ParamsSource`.</summary>
    public static IEnumerable<int> Counts => Profile.CatchUpCounts;

    /// <summary>Gets or sets how many events the sequence holds.</summary>
    [ParamsSource(nameof(Counts))]
    public int EventCount { get; set; }

    /// <summary>Starts the host and seeds the sequence outside measurement.</summary>
    /// <returns>The task.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        await StartHost();
        _storage = Host.CreateSequence();
        await Seed(_storage, EventCount);
    }

    /// <summary>Reads the whole sequence from the start.</summary>
    /// <returns>The number of events read.</returns>
    [Benchmark]
    public Task<int> FullDrain() => Measure(Key("catch-up-full", EventCount), count => count, async () => await Drain(await _storage.GetFromSequenceNumber(0)));

    /// <summary>Reads the whole sequence narrowed to one event type.</summary>
    /// <returns>The number of events read.</returns>
    [Benchmark]
    public Task<int> ByEventType() => Measure(Key("catch-up-by-type", EventCount), count => count, async () => await Drain(await _storage.GetRange(0, (ulong)(EventCount - 1), eventTypes: [BenchmarkEvents.Selected])));

    /// <summary>Reads from the middle of the sequence.</summary>
    /// <returns>The number of events read.</returns>
    [Benchmark]
    public Task<int> DeepCursor50() => Measure(Key("catch-up-deep-50", EventCount), count => count, async () => await Drain(await _storage.GetFromSequenceNumber((ulong)(EventCount / 2))));

    /// <summary>Reads the last tenth of the sequence.</summary>
    /// <returns>The number of events read.</returns>
    [Benchmark]
    public Task<int> DeepCursor90() => Measure(Key("catch-up-deep-90", EventCount), count => count, async () => await Drain(await _storage.GetFromSequenceNumber((ulong)(EventCount * 9 / 10))));
}
