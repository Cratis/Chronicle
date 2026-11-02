// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures live reactor handling across one or ten round-robin interleaved event sources.
/// </summary>
/// <remarks>
/// <para>
/// The measured window covers appending 1000 events in transactions of 100 and the reactor handling every event live.
/// The only registered observer is <see cref="BenchmarkReactor"/>. With one event source there is one partition run per
/// transaction; with ten round-robin sources every run is one event long, matching the shape of issue #5473.
/// </para>
/// <para>
/// The measured time includes waiting for completion, so it has a fixed floor that does not depend on how little
/// work the reactor does; compare the two cases with each other rather than reading absolute numbers.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 10, invocationCount: 1)]
public class ReactorLiveInterleaveBenchmark : IDisposable, IAsyncDisposable
{
    const int EventCount = 1000;
    const int TransactionSize = 100;
    static readonly TimeSpan _completionTimeout = TimeSpan.FromMinutes(10);

    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;

    /// <summary>
    /// Gets or sets the number of event sources interleaved in each transaction.
    /// </summary>
    [Params(1, 10)]
    public int EventSources { get; set; }

    /// <summary>
    /// Initializes the benchmark infrastructure and verifies the reactor is subscribed.
    /// </summary>
    /// <returns>A task that completes when setup and the priming check are finished.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the priming append or reactor delivery fails.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(
            _fixture,
            $"reactor-live-interleave-{EventSources}",
            new ObserverScopedClientArtifactsProvider(typeof(BenchmarkReactor)));
        await _helper.WaitForConnection();

        var before = BenchmarkReactor.TotalEventsProcessed;
        var source = EventSourceId.New();
        var result = await _helper.EventLog.Append(source, new BenchmarkEvent(source.Value, 0, DateTimeOffset.UtcNow));
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("The priming append failed; the reactor is not subscribed or ready for measurement.");
        }

        var completion = await result.WaitForCompletion(_completionTimeout);
        var delivered = BenchmarkReactor.TotalEventsProcessed - before;
        if (!completion.IsSuccess || delivered != 1)
        {
            throw new InvalidOperationException($"The reactor is not subscribed or failed the priming check: completion succeeded={completion.IsSuccess}, received={delivered}.");
        }
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure after the run completes.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <summary>
    /// Appends interleaved transactions without waiting between them and validates live reactor delivery.
    /// </summary>
    /// <returns>A task that completes when the reactor has handled all appended events.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an append, completion or delivery count check fails.</exception>
    [Benchmark]
    public async Task ReactToInterleavedEvents()
    {
        var sources = Enumerable.Range(0, EventSources).Select(_ => EventSourceId.New()).ToArray();
        var before = BenchmarkReactor.TotalEventsProcessed;
        AppendManyResult? last = null;

        for (var offset = 0; offset < EventCount; offset += TransactionSize)
        {
            var batch = new List<EventForEventSourceId>(TransactionSize);
            for (var i = offset; i < offset + TransactionSize; i++)
            {
                var source = sources[i % sources.Length];
                batch.Add(new EventForEventSourceId(source, new BenchmarkEvent(source.Value, i, DateTimeOffset.UtcNow)));
            }

            var result = await _helper!.EventLog.AppendMany(batch);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Append failed with {result.Errors.Count()} error(s), {result.ConstraintViolations.Count()} constraint violation(s) and {result.ConcurrencyViolations.Count()} concurrency violation(s).");
            }

            last = result;
        }

        var completion = await last!.WaitForCompletion(_completionTimeout);
        if (!completion.IsSuccess)
        {
            throw new InvalidOperationException($"The reactor did not complete: timed out={completion.TimedOut}, failed partitions={completion.FailedPartitions.Count()}, outstanding observers={string.Join(", ", completion.OutstandingObservers)}.");
        }

        var delivered = BenchmarkReactor.TotalEventsProcessed - before;
        if (delivered != EventCount)
        {
            throw new InvalidOperationException($"Expected the reactor to receive {EventCount} events across {EventSources} event source(s), but it received {delivered}.");
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
