// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Appends with concurrent writers and counts the commands per append. Writers share the sequence number counter,
/// so this is storage-level concurrency with the event sequence grain bypassed.
/// </summary>
[MemoryDiagnoser]
public class ConcurrentAppendBenchmark : AppendBenchmark
{
    IEventSequenceStorage _storage = null!;
    IEventSequenceStorage[] _storages = [];
    long _next;

    /// <summary>Gets the writer counts of this run, for `ParamsSource`.</summary>
    public static IEnumerable<int> Writers => Profile.Concurrency;

    /// <summary>Gets or sets the number of concurrent writers.</summary>
    [ParamsSource(nameof(Writers))]
    public int WriterCount { get; set; }

    /// <summary>Starts the host and creates the sequences.</summary>
    /// <returns>The task.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        await StartHost();
        _storage = Host.CreateSequence();
        _storages = [.. Enumerable.Range(0, WriterCount).Select(_ => Host.CreateSequence())];
        _next = 0;
    }

    /// <summary>Has every writer append one event to one shared sequence (sequence numbers from a shared atomic counter).</summary>
    /// <returns>The task.</returns>
    [Benchmark]
    public Task ConcurrentOneSequence() => Concurrent(Key("append-concurrent-one-sequence", WriterCount), _ => _storage, sharedSequence: true);

    /// <summary>Has every writer append one event to a sequence of its own.</summary>
    /// <returns>The task.</returns>
    [Benchmark]
    public Task ConcurrentManySequences() => Concurrent(Key("append-concurrent-many-sequences", WriterCount), index => _storages[index], sharedSequence: false);

    async Task Concurrent(string key, Func<int, IEventSequenceStorage> storageFor, bool sharedSequence)
    {
        var before = Host.Counter.Snapshot();
        var waveStarted = Stopwatch.GetTimestamp();
        var writers = Enumerable.Range(0, WriterCount).Select<int, Task<(long Started, TimeSpan Elapsed, Exception? Error)>>(async index =>
        {
            var started = Stopwatch.GetTimestamp();
            try
            {
                await Append(storageFor(index), (ulong)Interlocked.Increment(ref _next) - 1);
                return (Started: started, Elapsed: Stopwatch.GetElapsedTime(started), Error: null);
            }
            catch (Exception ex)
            {
                return (Started: started, Elapsed: Stopwatch.GetElapsedTime(started), Error: ex);
            }
        }).ToArray();
        var outcomes = await Task.WhenAll(writers);
        var latencies = outcomes.Where(_ => _.Error is null).Select(_ => _.Elapsed).ToArray();
        var failures = outcomes.Where(_ => _.Error is not null).Select(_ => _.Error!).ToArray();
        Measurements.Record(key, new InvocationRecord(latencies.Length, latencies.Length, Host.Counter.Snapshot().Since(before), latencies, failures.Length)
        {
            Samples = Measurements.IsCounting
                ? [.. outcomes.Select((outcome, index) => new WriterSample(index, sharedSequence ? 0 : index + 1, Stopwatch.GetElapsedTime(waveStarted, outcome.Started).TotalMilliseconds, outcome.Elapsed.TotalMilliseconds, outcome.Error is null))]
                : []
        });
        if (failures.Length > 0) throw new AggregateException(failures);
    }
}
