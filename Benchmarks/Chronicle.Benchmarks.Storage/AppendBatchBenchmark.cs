// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Appends batches to a fresh sequence and counts the commands per append.</summary>
[MemoryDiagnoser]
public class AppendBatchBenchmark : AppendBenchmark
{
    IEventSequenceStorage _storage = null!;
    long _next;

    /// <summary>Gets the batch sizes of this run, for `ParamsSource`.</summary>
    public static IEnumerable<int> Batches => Profile.BatchSizes;

    /// <summary>Gets or sets the number of events appended in one call.</summary>
    [ParamsSource(nameof(Batches))]
    public int BatchSize { get; set; }

    /// <summary>Starts the host and creates the sequence.</summary>
    /// <returns>The task.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        await StartHost();
        _storage = Host.CreateSequence();
        _next = 0;
    }

    /// <summary>Appends <see cref="BatchSize"/> events (a single append when it is 1, otherwise one AppendMany).</summary>
    /// <returns>The task.</returns>
    [Benchmark]
    public Task AppendBatch() => Measure(Key("append", BatchSize), _ => BatchSize, async () =>
    {
        if (BatchSize == 1)
        {
            await Append(_storage, (ulong)Interlocked.Increment(ref _next) - 1);
            return 1;
        }

        var first = (ulong)Interlocked.Add(ref _next, BatchSize) - (ulong)BatchSize;
        var result = await _storage.AppendMany(Enumerable.Range(0, BatchSize).Select(offset => BenchmarkEvents.ToAppend(first + (ulong)offset, Profile.PayloadBytes)));
        return result.IsSuccess ? BatchSize : throw new InvalidOperationException("Batch append failed.");
    });
}
