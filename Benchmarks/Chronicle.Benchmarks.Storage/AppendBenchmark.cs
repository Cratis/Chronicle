// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Dynamic;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using CorrelationId = Cratis.Execution.CorrelationId;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Appends to a fresh sequence: one event at a time, in batches, and with concurrent writers. Counts the commands
/// per append. Concurrent writers share the sequence number counter, so this is storage-level concurrency with the
/// event sequence grain bypassed.
/// </summary>
[MemoryDiagnoser]
public class AppendBenchmark : StorageBenchmark
{
    IEventSequenceStorage _storage = null!;
    IEventSequenceStorage[] _storages = [];
    long _next;

    /// <summary>Gets the batch sizes of this run, for `ParamsSource`.</summary>
    public static IEnumerable<int> Batches => Profile.BatchSizes;

    /// <summary>Gets the writer counts of this run, for `ParamsSource`.</summary>
    public static IEnumerable<int> Writers => Profile.Concurrency;

    /// <summary>Gets or sets the number of events appended in one call.</summary>
    [ParamsSource(nameof(Batches))]
    public int BatchSize { get; set; }

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

    /// <summary>Has every writer append one event to one shared sequence (sequence numbers from a shared atomic counter).</summary>
    /// <returns>The task.</returns>
    [Benchmark]
    public Task ConcurrentOneSequence() => Concurrent(Key("append-concurrent-one-sequence", WriterCount), _ => _storage);

    /// <summary>Has every writer append one event to a sequence of its own.</summary>
    /// <returns>The task.</returns>
    [Benchmark]
    public Task ConcurrentManySequences() => Concurrent(Key("append-concurrent-many-sequences", WriterCount), index => _storages[index]);

    static async Task Append(IEventSequenceStorage storage, ulong sequenceNumber)
    {
        var type = BenchmarkEvents.TypeAt(sequenceNumber);
        var content = new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = BenchmarkEvents.Content(Profile.PayloadBytes) };
        var hashes = new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "hash" };
        var result = await storage.Append(sequenceNumber, EventSourceType.Default, BenchmarkEvents.SourceAt(sequenceNumber), EventStreamType.All, EventStreamId.Default, type, CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, content, hashes);
        if (!result.IsSuccess) throw new InvalidOperationException("Append failed.");
    }

    async Task Concurrent(string key, Func<int, IEventSequenceStorage> storageFor)
    {
        var stats = Measurements.For(key);
        var before = Host.Counter.Snapshot();
        var writers = Enumerable.Range(0, WriterCount).Select(async index =>
        {
            var started = Stopwatch.GetTimestamp();
            try
            {
                await Append(storageFor(index), (ulong)Interlocked.Increment(ref _next) - 1);
                stats.RecordLatency(Stopwatch.GetElapsedTime(started));
            }
            catch (Exception)
            {
                stats.RecordError();
            }
        }).ToArray();
        await Task.WhenAll(writers);
        stats.RecordBatch(WriterCount, WriterCount, Host.Counter.Snapshot().Since(before));
    }
}
