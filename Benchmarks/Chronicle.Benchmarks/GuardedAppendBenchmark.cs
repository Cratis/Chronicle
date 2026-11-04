// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Compares unchecked, default-strategy, explicit-scope and decision-guarded single-event appends.
/// </summary>
/// <remarks>
/// Each path has its own seeded key. Explicit boundaries and decision reads are captured outside the measured window.
/// Each append lengthens its key's history by one event.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 5, invocationCount: 1)]
public class GuardedAppendBenchmark : IDisposable, IAsyncDisposable
{
    ChronicleBenchmarkFixture? _fixture;
    ChronicleClientHelper? _helper;
    IDecisionReads _reads = null!;
    Guid _unguardedKey;
    Guid _defaultKey;
    Guid _explicitKey;
    Guid _decisionKey;
    EventType _eventType = null!;
    EventSequenceNumber _boundary = EventSequenceNumber.Unavailable;
    DecisionRead<BenchmarkMaterializedReadModel> _read = null!;
    int _nextValue;

    /// <summary>
    /// Gets or sets the initial number of events in each key's history.
    /// </summary>
    [Params(10, 1000)]
    public int HistoryLength { get; set; }

    /// <summary>
    /// Seeds independent histories, validates reads and verifies the append paths' concurrency checks.
    /// </summary>
    /// <returns>A task that completes when setup is finished.</returns>
    /// <exception cref="InvalidOperationException">Thrown when seeding, reads or append-path checks fail.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new ChronicleBenchmarkFixture();
        _helper = new ChronicleClientHelper(
            _fixture,
            $"guarded-appends-{HistoryLength}",
            new ObserverScopedClientArtifactsProvider(typeof(BenchmarkMaterializedReadModel)));
        await _helper.WaitForConnection();
        _reads = _helper.EventStore.GetDecisionReads();
        _eventType = _helper.EventStore.EventTypes.GetEventTypeFor(typeof(BenchmarkInstanceRecorded));
        _unguardedKey = Guid.NewGuid();
        _defaultKey = Guid.NewGuid();
        _explicitKey = Guid.NewGuid();
        _decisionKey = Guid.NewGuid();
        _nextValue = HistoryLength;
        foreach (var key in new[] { _unguardedKey, _defaultKey, _explicitKey, _decisionKey })
        {
            await DecisionBenchmarkSeeding.Seed(_helper.EventLog, key, HistoryLength);
        }

        await DecisionBenchmarkSeeding.Validate(_helper.EventStore, _reads, _decisionKey, HistoryLength - 1);
        ValidateConcurrencyCheck(await Unguarded(), false, nameof(Unguarded));
        await DefaultStrategy();
        CaptureExplicitBoundary();
        ValidateConcurrencyCheck(await ExplicitScope(), true, nameof(ExplicitScope));
        CaptureDecisionRead();
        ValidateConcurrencyCheck(await DecisionGuarded(), true, nameof(DecisionGuarded));
    }

    /// <summary>
    /// Captures a source- and event-type-scoped boundary before an explicit-scope iteration.
    /// </summary>
    [IterationSetup(Target = nameof(ExplicitScope))]
    public void CaptureExplicitBoundary() => _boundary = _helper!.EventLog
        .GetTailSequenceNumber(_explicitKey, filterEventTypes: [_eventType]).GetAwaiter().GetResult();

    /// <summary>
    /// Captures a fresh detached decision read before a guarded iteration.
    /// </summary>
    [IterationSetup(Target = nameof(DecisionGuarded))]
    public void CaptureDecisionRead() => _read = _reads.GetDetached<BenchmarkMaterializedReadModel>(_decisionKey).GetAwaiter().GetResult();

    /// <summary>
    /// Appends without a client tail read or a server concurrency check.
    /// </summary>
    /// <returns>Whether the successful append performed a concurrency check.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the append fails.</exception>
    [Benchmark(Baseline = true)]
    public async Task<bool> Unguarded()
    {
        var result = await _helper!.EventLog.AppendMany(
            [new EventForEventSourceId(_unguardedKey, NextEvent())],
            concurrencyScopes: new Dictionary<EventSourceId, ConcurrencyScope> { [_unguardedKey] = ConcurrencyScope.None });
        return ValidateAppend(result.IsSuccess, result.ConcurrencyCheckPerformed, nameof(Unguarded));
    }

    /// <summary>
    /// Appends using the default strategy's client tail read and server validation.
    /// </summary>
    /// <returns>Whether the successful append performed a concurrency check.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the append fails.</exception>
    [Benchmark]
    public async Task<bool> DefaultStrategy()
    {
        var result = await _helper!.EventLog.AppendMany([new EventForEventSourceId(_defaultKey, NextEvent())]);
        return ValidateAppend(result.IsSuccess, result.ConcurrencyCheckPerformed, nameof(DefaultStrategy));
    }

    /// <summary>
    /// Appends against the boundary captured outside the measured window.
    /// </summary>
    /// <remarks>
    /// Unlike the decision guard, this public explicit scope does not carry a MutationEpoch for revisions or redactions.
    /// </remarks>
    /// <returns>Whether the successful append performed a concurrency check.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the append fails.</exception>
    [Benchmark]
    public async Task<bool> ExplicitScope()
    {
        var result = await _helper!.EventLog.AppendMany(
            [new EventForEventSourceId(_explicitKey, NextEvent())],
            concurrencyScopes: new Dictionary<EventSourceId, ConcurrencyScope>
            {
                [_explicitKey] = new ConcurrencyScope(_boundary, _explicitKey, EventTypes: [_eventType])
            });
        return ValidateAppend(result.IsSuccess, result.ConcurrencyCheckPerformed, nameof(ExplicitScope));
    }

    /// <summary>
    /// Appends guarded by the fresh decision read captured outside the measured window.
    /// </summary>
    /// <returns>Whether the successful append performed a concurrency check.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the append fails, including the decision-conflict count.</exception>
    [Benchmark]
    public async Task<bool> DecisionGuarded()
    {
        var result = await _helper!.EventLog.AppendMany(
            [new EventForEventSourceId(_decisionKey, NextEvent())], guardedBy: [_read]);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"The decision-guarded append failed with {result.GetDecisionConflicts([_read]).Count()} decision conflict(s).");
        }

        return result.ConcurrencyCheckPerformed;
    }

    /// <summary>
    /// Cleans up the benchmark infrastructure.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public Task Cleanup() => DisposeAsync().AsTask();

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously disposes the benchmark resources.
    /// </summary>
    /// <returns>A value task that completes when resources have been disposed.</returns>
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

    /// <summary>
    /// Throws when an append failed and returns whether it performed a concurrency check.
    /// </summary>
    /// <remarks>
    /// The client and kernel assemblies both define an AppendManyResult type in the same namespace, so the helpers
    /// take its values rather than naming the type.
    /// </remarks>
    /// <param name="isSuccess">Whether the append succeeded.</param>
    /// <param name="concurrencyCheckPerformed">Whether the append performed a concurrency check.</param>
    /// <param name="path">The benchmark path, for the error message.</param>
    /// <returns>The value of <paramref name="concurrencyCheckPerformed"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the append failed.</exception>
    static bool ValidateAppend(bool isSuccess, bool concurrencyCheckPerformed, string path)
    {
        if (!isSuccess)
        {
            throw new InvalidOperationException($"The {path} append failed.");
        }

        return concurrencyCheckPerformed;
    }

    static void ValidateConcurrencyCheck(bool concurrencyCheckPerformed, bool expected, string path)
    {
        if (concurrencyCheckPerformed != expected)
        {
            throw new InvalidOperationException($"The {path} append reported ConcurrencyCheckPerformed={concurrencyCheckPerformed}, expected {expected}.");
        }
    }

    BenchmarkInstanceRecorded NextEvent()
    {
        var value = _nextValue++;
        return new($"Benchmark{value}", value, DateTimeOffset.UtcNow);
    }
}
