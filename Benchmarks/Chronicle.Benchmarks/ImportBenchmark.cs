// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures importing a large history into an event log through an import session (decision 0023), reporting the
/// staging and the commit throughput separately.
/// </summary>
[MemoryDiagnoser]
[InProcess]
[SimpleJob(warmupCount: 0, iterationCount: 3, invocationCount: 1)]
public class ImportBenchmark : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// The number of events imported in each invocation.
    /// </summary>
    const int EventCount = 300_000;

    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    int _run;

    /// <summary>
    /// Initializes the benchmark fixture and Chronicle client.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(_fixture, ChronicleClientHelper.DefaultEventStoreName, AppendManyBenchmark.ArtifactsProvider);
        await _helper.WaitForConnection();
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure after the run completes.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <summary>
    /// Imports the events and waits for the commit, reporting both phases.
    /// </summary>
    /// <returns>A task that completes when the import is committed.</returns>
    /// <exception cref="InvalidOperationException">The import did not complete.</exception>
    [Benchmark]
    public async Task ImportEvents()
    {
        var run = ++_run;
        var staging = Stopwatch.StartNew();
        await using var import = await _helper!.EventLog.BeginImport(new ImportOptions { WaitForCommit = false });
        var completed = import.Progress.FirstAsync(_ => _.State is ImportState.Completed or ImportState.Failed).ToTask();
        await import.Add(Events(run, EventCount));
        var closed = await import.Complete();
        staging.Stop();

        var commit = Stopwatch.StartNew();
        var final = await completed;
        commit.Stop();

        if (final.State != ImportState.Completed || final.EventsCommitted != EventCount)
        {
            throw new InvalidOperationException($"Import {closed.ImportId} ended {final.State} with {final.EventsCommitted} of {EventCount} events committed.");
        }

        Console.WriteLine($"Staged {EventCount} events in {staging.Elapsed} ({EventCount / staging.Elapsed.TotalSeconds:F0} events/s); committed in {commit.Elapsed} ({EventCount / commit.Elapsed.TotalSeconds:F0} events/s).");
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

    static async IAsyncEnumerable<EventForEventSourceId> Events(int run, int count)
    {
        for (var index = 0; index < count; index++)
        {
            if (index % 10_000 == 0) await Task.Yield();
            yield return new EventForEventSourceId($"import-{run}-{index % 1000}", new BenchmarkEvent($"Imported{index}", index, DateTimeOffset.UnixEpoch.AddSeconds(index)))
            {
                Occurred = DateTimeOffset.UnixEpoch.AddSeconds(index),
                SourceSequenceNumber = (ulong)index
            };
        }
    }
}
